namespace Enterprise.Agent.Shared.Constants;

/// <summary>Cross-cutting constant values shared by every layer of the platform.</summary>
public static class PlatformConstants
{
    /// <summary>HTTP header carrying the correlation id in and out of the API.</summary>
    public const string CorrelationHeader = "X-Correlation-Id";

    /// <summary>HTTP header carrying an API key for machine-to-machine access.</summary>
    public const string ApiKeyHeader = "X-Api-Key";

    public const string ProductName = "Enterprise Multi-Agent AI Platform";

    public static class Policies
    {
        public const string RequireUser = "RequireUser";
        public const string RequireAdmin = "RequireAdmin";
        public const string RequireAgentOperator = "RequireAgentOperator";
        public const string ApiKeyOrJwt = "ApiKeyOrJwt";
    }

    public static class Roles
    {
        public const string Admin = "admin";
        public const string Operator = "operator";
        public const string User = "user";
    }

    public static class ClaimTypes
    {
        public const string TenantId = "tenant_id";
        public const string ApiKey = "api_key";
    }
}
