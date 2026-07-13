namespace Enterprise.Agent.Ui.Options;

/// <summary>Configuration for the UI's connection to the platform REST API.</summary>
public sealed class ApiClientOptions
{
    public const string SectionName = "Api";

    /// <summary>Base URL of the Enterprise Agent API (server-side calls).</summary>
    public string BaseUrl { get; set; } = "http://localhost:5080";

    /// <summary>API key sent as X-Api-Key on server-side calls (kept off the browser).</summary>
    public string ApiKey { get; set; } = "dev-local-api-key-change-me";

    /// <summary>
    /// Base URL the browser uses to reach the API's SignalR hub. Defaults to <see cref="BaseUrl"/>.
    /// </summary>
    public string? PublicBaseUrl { get; set; }

    public string ResolvePublicBaseUrl() =>
        string.IsNullOrWhiteSpace(PublicBaseUrl) ? BaseUrl : PublicBaseUrl!;
}
