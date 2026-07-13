using System.ComponentModel;
using System.Text;
using Enterprise.Agent.Core.Abstractions.Tools;
using Enterprise.Agent.Tools.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace Enterprise.Agent.Tools;

/// <summary>
/// Generic REST client plugin allowing agents to call external HTTP APIs. Enforces an
/// optional host allow-list and a request timeout.
/// </summary>
public sealed class RestApiTool : IKernelPluginSource
{
    public const string PluginName = "RestApi";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly RestApiToolOptions _options;
    private readonly ILogger<RestApiTool> _logger;

    public RestApiTool(
        IHttpClientFactory httpClientFactory,
        IOptions<RestApiToolOptions> options,
        ILogger<RestApiTool> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    [KernelFunction("http_get"), Description("Performs an HTTP GET request and returns the response body.")]
    public Task<string> GetAsync(
        [Description("Absolute URL to request.")] string url,
        CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Get, url, null, null, cancellationToken);

    [KernelFunction("http_post"), Description("Performs an HTTP POST request with a JSON body and returns the response body.")]
    public Task<string> PostAsync(
        [Description("Absolute URL to request.")] string url,
        [Description("Request body (typically JSON).")] string body,
        [Description("Content type, e.g. application/json.")] string contentType = "application/json",
        CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Post, url, body, contentType, cancellationToken);

    public KernelPlugin BuildPlugin() => KernelPluginFactory.CreateFromObject(this, PluginName);

    private async Task<string> SendAsync(
        HttpMethod method, string url, string? body, string? contentType, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return $"'{url}' is not a valid absolute URL.";
        }

        if (_options.AllowedHosts.Count > 0
            && !_options.AllowedHosts.Any(h => string.Equals(h, uri.Host, StringComparison.OrdinalIgnoreCase)))
        {
            return $"Host '{uri.Host}' is not in the allow-list.";
        }

        try
        {
            var client = _httpClientFactory.CreateClient(nameof(RestApiTool));
            client.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

            using var request = new HttpRequestMessage(method, uri);
            if (body is not null)
            {
                request.Content = new StringContent(body, Encoding.UTF8, contentType ?? "application/json");
            }

            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var trimmed = content.Length > 8000 ? content[..8000] + "…(truncated)" : content;
            return $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}\n{trimmed}";
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "REST call to {Url} failed.", url);
            return $"Request to '{url}' failed: {ex.Message}";
        }
    }
}
