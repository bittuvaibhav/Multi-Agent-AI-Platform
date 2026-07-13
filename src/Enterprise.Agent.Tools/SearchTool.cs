using System.ComponentModel;
using System.Net.Http.Headers;
using System.Text.Json;
using Enterprise.Agent.Core.Abstractions.Tools;
using Enterprise.Agent.Tools.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace Enterprise.Agent.Tools;

/// <summary>
/// Web search plugin. Calls a configurable search endpoint (Bing/SerpAPI-compatible). When
/// not configured it returns a clear, non-fatal message so agents can proceed.
/// </summary>
public sealed class SearchTool : IKernelPluginSource
{
    public const string PluginName = "Search";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SearchToolOptions _options;
    private readonly ILogger<SearchTool> _logger;

    public SearchTool(
        IHttpClientFactory httpClientFactory,
        IOptions<SearchToolOptions> options,
        ILogger<SearchTool> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    [KernelFunction("web_search"), Description("Searches the web and returns the top result snippets.")]
    public async Task<string> SearchAsync(
        [Description("The search query.")] string query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return "A search query must be provided.";
        }

        if (string.IsNullOrWhiteSpace(_options.Endpoint))
        {
            _logger.LogInformation("Search tool invoked but no endpoint is configured.");
            return "Web search is not configured in this environment.";
        }

        try
        {
            var client = _httpClientFactory.CreateClient(nameof(SearchTool));
            var url = $"{_options.Endpoint}?q={Uri.EscapeDataString(query)}&count={_options.MaxResults}";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
                request.Headers.TryAddWithoutValidation("Ocp-Apim-Subscription-Key", _options.ApiKey);
            }

            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return $"Search returned status {(int)response.StatusCode}.";
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return SummarizeResults(json);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Web search failed for query {Query}.", query);
            return "Web search failed to return results.";
        }
    }

    public KernelPlugin BuildPlugin() => KernelPluginFactory.CreateFromObject(this, PluginName);

    private static string SummarizeResults(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            // Best-effort extraction of common shapes: {webPages:{value:[{name,snippet,url}]}} or {organic:[...]}.
            if (doc.RootElement.TryGetProperty("webPages", out var web)
                && web.TryGetProperty("value", out var value))
            {
                return string.Join("\n", value.EnumerateArray().Take(5).Select(FormatItem));
            }

            if (doc.RootElement.TryGetProperty("organic", out var organic))
            {
                return string.Join("\n", organic.EnumerateArray().Take(5).Select(FormatItem));
            }

            return json.Length > 2000 ? json[..2000] : json;
        }
        catch (JsonException)
        {
            return json.Length > 2000 ? json[..2000] : json;
        }
    }

    private static string FormatItem(JsonElement item)
    {
        var name = item.TryGetProperty("name", out var n) ? n.GetString()
            : item.TryGetProperty("title", out var t) ? t.GetString() : "(result)";
        var snippet = item.TryGetProperty("snippet", out var s) ? s.GetString() : string.Empty;
        return $"- {name}: {snippet}";
    }
}
