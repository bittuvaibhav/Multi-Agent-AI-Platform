using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Enterprise.Agent.Core.Abstractions.VectorStore;
using Enterprise.Agent.Models.Enums;
using Enterprise.Agent.Models.VectorStore;
using Enterprise.Agent.VectorStore.Options;
using Microsoft.Extensions.Logging;

namespace Enterprise.Agent.VectorStore.AzureSearch;

/// <summary>
/// Azure AI Search implementation of <see cref="IVectorStore"/> using the service's REST API
/// directly (no SDK version coupling). Each collection maps to a search index that carries a
/// vector field named <c>embedding</c> plus <c>content</c> and <c>metadata</c> fields.
/// </summary>
public sealed class AzureAiSearchVectorStore : IVectorStore
{
    private const string VectorProfile = "vprofile";
    private const string VectorAlgorithm = "valgo";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AzureSearchVectorOptions _options;
    private readonly ILogger<AzureAiSearchVectorStore> _logger;

    public AzureAiSearchVectorStore(
        IHttpClientFactory httpClientFactory,
        AzureSearchVectorOptions options,
        ILogger<AzureAiSearchVectorStore> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    public VectorProvider Provider => VectorProvider.AzureAiSearch;

    public async Task EnsureCollectionAsync(string collection, int dimensions, CancellationToken cancellationToken = default)
    {
        var index = IndexName(collection);
        var definition = new JsonObject
        {
            ["name"] = index,
            ["fields"] = new JsonArray
            {
                new JsonObject { ["name"] = "id", ["type"] = "Edm.String", ["key"] = true, ["filterable"] = true },
                new JsonObject { ["name"] = "content", ["type"] = "Edm.String", ["searchable"] = true },
                new JsonObject { ["name"] = "metadata", ["type"] = "Edm.String", ["retrievable"] = true },
                new JsonObject
                {
                    ["name"] = "embedding",
                    ["type"] = "Collection(Edm.Single)",
                    ["searchable"] = true,
                    ["dimensions"] = dimensions,
                    ["vectorSearchProfile"] = VectorProfile
                }
            },
            ["vectorSearch"] = new JsonObject
            {
                ["algorithms"] = new JsonArray
                {
                    new JsonObject { ["name"] = VectorAlgorithm, ["kind"] = "hnsw" }
                },
                ["profiles"] = new JsonArray
                {
                    new JsonObject { ["name"] = VectorProfile, ["algorithm"] = VectorAlgorithm }
                }
            }
        };

        var client = CreateClient();
        using var response = await client.PutAsync(
            $"indexes/{index}?api-version={_options.ApiVersion}",
            JsonContent.Create(definition),
            cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogWarning("Azure AI Search index create returned {Status}: {Body}", response.StatusCode, body);
        }
    }

    public async Task UpsertAsync(IReadOnlyList<VectorRecord> records, CancellationToken cancellationToken = default)
    {
        if (records.Count == 0)
        {
            return;
        }

        var client = CreateClient();
        foreach (var group in records.GroupBy(r => r.Collection))
        {
            var index = IndexName(group.Key);
            var actions = new JsonArray();
            foreach (var record in group)
            {
                actions.Add(new JsonObject
                {
                    ["@search.action"] = "mergeOrUpload",
                    ["id"] = record.Id,
                    ["content"] = record.Content,
                    ["metadata"] = JsonSerializer.Serialize(record.Metadata),
                    ["embedding"] = new JsonArray(record.Embedding.Select(f => JsonValue.Create(f)).ToArray())
                });
            }

            var payload = new JsonObject { ["value"] = actions };
            using var response = await client.PostAsync(
                $"indexes/{index}/docs/index?api-version={_options.ApiVersion}",
                JsonContent.Create(payload),
                cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogWarning("Azure AI Search upsert returned {Status}: {Body}", response.StatusCode, body);
            }
        }
    }

    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        VectorQuery query, CancellationToken cancellationToken = default)
    {
        var index = IndexName(query.Collection);
        var payload = new JsonObject
        {
            ["vectorQueries"] = new JsonArray
            {
                new JsonObject
                {
                    ["kind"] = "vector",
                    ["vector"] = new JsonArray(query.Embedding.Select(f => JsonValue.Create(f)).ToArray()),
                    ["fields"] = "embedding",
                    ["k"] = query.TopK
                }
            },
            ["select"] = "id,content,metadata",
            ["top"] = query.TopK
        };

        var client = CreateClient();
        using var response = await client.PostAsync(
            $"indexes/{index}/docs/search?api-version={_options.ApiVersion}",
            JsonContent.Create(payload),
            cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogWarning("Azure AI Search query returned {Status}: {Body}", response.StatusCode, body);
            return [];
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return ParseResults(json, query.MinScore);
    }

    public async Task DeleteAsync(string collection, IReadOnlyList<string> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return;
        }

        var index = IndexName(collection);
        var actions = new JsonArray(
            ids.Select(id => (JsonNode)new JsonObject { ["@search.action"] = "delete", ["id"] = id }).ToArray());
        var payload = new JsonObject { ["value"] = actions };

        var client = CreateClient();
        using var response = await client.PostAsync(
            $"indexes/{index}/docs/index?api-version={_options.ApiVersion}",
            JsonContent.Create(payload),
            cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    private static IReadOnlyList<VectorSearchResult> ParseResults(string json, double minScore)
    {
        var results = new List<VectorSearchResult>();
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("value", out var value))
        {
            return results;
        }

        foreach (var item in value.EnumerateArray())
        {
            var score = item.TryGetProperty("@search.score", out var s) ? s.GetDouble() : 0d;
            if (score < minScore)
            {
                continue;
            }

            var metadataJson = item.TryGetProperty("metadata", out var m) ? m.GetString() ?? "{}" : "{}";
            results.Add(new VectorSearchResult
            {
                Id = item.TryGetProperty("id", out var id) ? id.GetString() ?? string.Empty : string.Empty,
                Content = item.TryGetProperty("content", out var c) ? c.GetString() ?? string.Empty : string.Empty,
                Score = score,
                Metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(metadataJson) ?? new()
            });
        }

        return results;
    }

    private HttpClient CreateClient()
    {
        var client = _httpClientFactory.CreateClient(nameof(AzureAiSearchVectorStore));
        client.BaseAddress = new Uri(_options.Endpoint.TrimEnd('/') + "/");
        client.DefaultRequestHeaders.Remove("api-key");
        client.DefaultRequestHeaders.Add("api-key", _options.ApiKey);
        return client;
    }

    private static string IndexName(string collection)
    {
        var safe = new string((collection ?? "default").ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray()).Trim('-');
        return string.IsNullOrEmpty(safe) ? "default" : safe;
    }
}
