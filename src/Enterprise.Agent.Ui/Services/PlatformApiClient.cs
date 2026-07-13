using System.Net.Http.Json;
using System.Text.Json;
using Enterprise.Agent.Models.Agents;
using Enterprise.Agent.Models.Chat;
using Enterprise.Agent.Models.Orchestration;
using Enterprise.Agent.Models.Rag;
using Enterprise.Agent.Models.Sql;
using Enterprise.Agent.Models.Memory;
using Enterprise.Agent.Models.Tools;

namespace Enterprise.Agent.Ui.Services;

/// <summary>The outcome of an API call: a value on success, or an error message.</summary>
public sealed record ApiResponse<T>(bool Success, T? Value, string? Error)
{
    public static ApiResponse<T> Ok(T value) => new(true, value, null);
    public static ApiResponse<T> Fail(string error) => new(false, default, error);
}

/// <summary>
/// Typed HTTP client for the Enterprise Agent REST API. The API key is attached by the DI
/// registration, so it never reaches the browser. All calls degrade to a friendly error
/// message instead of throwing into the page.
/// </summary>
public sealed class PlatformApiClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly ILogger<PlatformApiClient> _logger;

    public PlatformApiClient(HttpClient http, ILogger<PlatformApiClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    // ----- Health & agents ------------------------------------------------------------------

    public Task<ApiResponse<JsonElement>> GetHealthAsync(CancellationToken ct = default) =>
        GetAsync<JsonElement>("/api/health", ct);

    public Task<ApiResponse<List<AgentDescriptor>>> GetAgentsAsync(CancellationToken ct = default) =>
        GetAsync<List<AgentDescriptor>>("/api/agents", ct);

    public Task<ApiResponse<WorkflowPlan>> PlanAsync(string goal, CancellationToken ct = default) =>
        PostAsync<WorkflowPlan>("/api/agents/plan", new { goal }, ct);

    public Task<ApiResponse<AgentResponse>> RunAgentAsync(
        string name, string input, CancellationToken ct = default) =>
        PostAsync<AgentResponse>($"/api/agents/{Uri.EscapeDataString(name)}", new { input }, ct);

    // ----- Chat (REST fallback; the Chat page uses SignalR) ---------------------------------

    public Task<ApiResponse<ChatResponse>> ChatAsync(
        string message, string? preferredAgent, CancellationToken ct = default) =>
        PostAsync<ChatResponse>("/api/chat", new { message, preferredAgent }, ct);

    // ----- RAG ------------------------------------------------------------------------------

    public Task<ApiResponse<RagContext>> RagQueryAsync(
        string query, string? collection, int topK, CancellationToken ct = default) =>
        PostAsync<RagContext>("/api/rag/query", new { query, collection, topK }, ct);

    public Task<ApiResponse<RagAnswerDto>> RagAnswerAsync(
        string query, string? collection, CancellationToken ct = default) =>
        PostAsync<RagAnswerDto>("/api/rag/answer", new { query, collection }, ct);

    public Task<ApiResponse<IngestionResult>> IngestAsync(
        string documentId, string fileName, int documentType, string contentBase64, string? collection,
        CancellationToken ct = default) =>
        PostAsync<IngestionResult>("/api/documents/ingest",
            new { documentId, fileName, documentType, contentBase64, collection }, ct);

    public Task<ApiResponse<List<DocumentDto>>> ListDocumentsAsync(
        string? collection, CancellationToken ct = default) =>
        GetAsync<List<DocumentDto>>(
            "/api/documents" + (string.IsNullOrWhiteSpace(collection) ? "" : $"?collection={Uri.EscapeDataString(collection)}"), ct);

    // ----- SQL ------------------------------------------------------------------------------

    public Task<ApiResponse<SqlAgentResult>> SqlQueryAsync(
        string question, string? dataSource, int maxRows, CancellationToken ct = default) =>
        PostAsync<SqlAgentResult>("/api/sql/query", new { question, dataSource, maxRows }, ct);

    // ----- Memory ---------------------------------------------------------------------------

    public Task<ApiResponse<List<MemoryRecord>>> GetConversationMemoryAsync(
        string conversationId, int limit, CancellationToken ct = default) =>
        GetAsync<List<MemoryRecord>>(
            $"/api/memory/conversation/{Uri.EscapeDataString(conversationId)}?limit={limit}", ct);

    public Task<ApiResponse<List<MemoryRecord>>> RecallMemoryAsync(
        string query, int limit, CancellationToken ct = default) =>
        GetAsync<List<MemoryRecord>>(
            $"/api/memory/semantic/recall?query={Uri.EscapeDataString(query)}&limit={limit}", ct);

    public async Task<ApiResponse<bool>> RememberAsync(
        string key, string content, string? conversationId, CancellationToken ct = default)
    {
        try
        {
            using var response = await _http.PostAsJsonAsync(
                "/api/memory/semantic", new { key, content, conversationId }, Json, ct).ConfigureAwait(false);
            return response.IsSuccessStatusCode
                ? ApiResponse<bool>.Ok(true)
                : ApiResponse<bool>.Fail(await DescribeErrorAsync(response, ct).ConfigureAwait(false));
        }
        catch (Exception ex)
        {
            return Failure<bool>(ex);
        }
    }

    // ----- Tools ----------------------------------------------------------------------------

    public Task<ApiResponse<List<ToolDescriptor>>> GetToolsAsync(CancellationToken ct = default) =>
        GetAsync<List<ToolDescriptor>>("/api/tools", ct);

    public Task<ApiResponse<ToolResult>> InvokeToolAsync(
        string plugin, string function, Dictionary<string, string> arguments, CancellationToken ct = default) =>
        PostAsync<ToolResult>("/api/tools/invoke", new { plugin, function, arguments }, ct);

    // ----- Auth -----------------------------------------------------------------------------

    public Task<ApiResponse<TokenDto>> IssueTokenAsync(
        string subject, string[] roles, CancellationToken ct = default) =>
        PostAsync<TokenDto>("/api/auth/token", new { subject, roles }, ct);

    // ----- Plumbing -------------------------------------------------------------------------

    private async Task<ApiResponse<T>> GetAsync<T>(string path, CancellationToken ct)
    {
        try
        {
            using var response = await _http.GetAsync(path, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return ApiResponse<T>.Fail(await DescribeErrorAsync(response, ct).ConfigureAwait(false));
            }

            var value = await response.Content.ReadFromJsonAsync<T>(Json, ct).ConfigureAwait(false);
            return value is null ? ApiResponse<T>.Fail("Empty response.") : ApiResponse<T>.Ok(value);
        }
        catch (Exception ex)
        {
            return Failure<T>(ex);
        }
    }

    private async Task<ApiResponse<T>> PostAsync<T>(string path, object body, CancellationToken ct)
    {
        try
        {
            using var response = await _http.PostAsJsonAsync(path, body, Json, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return ApiResponse<T>.Fail(await DescribeErrorAsync(response, ct).ConfigureAwait(false));
            }

            var value = await response.Content.ReadFromJsonAsync<T>(Json, ct).ConfigureAwait(false);
            return value is null ? ApiResponse<T>.Fail("Empty response.") : ApiResponse<T>.Ok(value);
        }
        catch (Exception ex)
        {
            return Failure<T>(ex);
        }
    }

    private ApiResponse<T> Failure<T>(Exception ex)
    {
        _logger.LogWarning(ex, "API call failed.");
        return ApiResponse<T>.Fail(
            $"Could not reach the API. Is it running and is '{_http.BaseAddress}' correct? ({ex.Message})");
    }

    private static async Task<string> DescribeErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var detail = string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body;
        return $"API returned {(int)response.StatusCode}: {detail}";
    }
}
