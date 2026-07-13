namespace Enterprise.Agent.Tools.Options;

/// <summary>Configuration for the web search tool.</summary>
public sealed class SearchToolOptions
{
    public const string SectionName = "Tools:Search";

    /// <summary>Search endpoint (e.g. a Bing/SerpAPI-compatible URL). When empty the tool degrades gracefully.</summary>
    public string Endpoint { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public int MaxResults { get; set; } = 5;
}

/// <summary>Configuration for the weather tool.</summary>
public sealed class WeatherToolOptions
{
    public const string SectionName = "Tools:Weather";

    /// <summary>Format string for a keyless weather endpoint; {0} is replaced with the location.</summary>
    public string EndpointFormat { get; set; } = "https://wttr.in/{0}?format=j1";
}

/// <summary>Configuration for the generic REST API tool.</summary>
public sealed class RestApiToolOptions
{
    public const string SectionName = "Tools:RestApi";

    /// <summary>Optional allow-list of host names the tool may call. Empty means allow all.</summary>
    public List<string> AllowedHosts { get; set; } = [];

    public int TimeoutSeconds { get; set; } = 30;
}

/// <summary>Configuration for the sandboxed file tool.</summary>
public sealed class FileToolOptions
{
    public const string SectionName = "Tools:File";

    /// <summary>Root directory the tool is confined to. Defaults to a workspace folder.</summary>
    public string BasePath { get; set; } = Path.Combine(Path.GetTempPath(), "enterprise-agent-files");

    public long MaxReadBytes { get; set; } = 1_048_576; // 1 MiB
}
