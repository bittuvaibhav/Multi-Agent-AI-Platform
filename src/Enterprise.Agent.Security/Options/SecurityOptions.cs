namespace Enterprise.Agent.Security.Options;

/// <summary>JWT bearer configuration.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "enterprise-agent";

    public string Audience { get; set; } = "enterprise-agent-clients";

    /// <summary>HMAC signing key. Must be supplied via secure configuration in production.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public int ExpiryMinutes { get; set; } = 60;
}

/// <summary>API-key authentication configuration.</summary>
public sealed class ApiKeyOptions
{
    public const string SectionName = "ApiKeys";

    /// <summary>Valid API keys mapped to a display/owner name.</summary>
    public Dictionary<string, string> Keys { get; set; } = new();
}
