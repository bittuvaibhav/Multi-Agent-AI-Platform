using System.Security.Claims;
using System.Text.Encodings.Web;
using Enterprise.Agent.Security.Options;
using Enterprise.Agent.Shared.Constants;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Enterprise.Agent.Security.ApiKey;

/// <summary>Validates API keys against the configured set.</summary>
public interface IApiKeyValidator
{
    bool TryValidate(string apiKey, out string owner);
}

public sealed class ApiKeyValidator : IApiKeyValidator
{
    private readonly ApiKeyOptions _options;

    public ApiKeyValidator(IOptions<ApiKeyOptions> options) => _options = options.Value;

    public bool TryValidate(string apiKey, out string owner)
    {
        owner = string.Empty;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return false;
        }

        if (_options.Keys.TryGetValue(apiKey, out var name))
        {
            owner = name;
            return true;
        }

        return false;
    }
}

/// <summary>
/// Authentication handler for the <c>X-Api-Key</c> header. Successful validation yields a
/// principal in the operator role, enabling machine-to-machine access alongside JWT.
/// </summary>
public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "ApiKey";

    private readonly IApiKeyValidator _validator;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IApiKeyValidator validator)
        : base(options, logger, encoder)
    {
        _validator = validator;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(PlatformConstants.ApiKeyHeader, out var values))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var apiKey = values.ToString();
        if (!_validator.TryValidate(apiKey, out var owner))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, owner),
            new Claim(ClaimTypes.Role, PlatformConstants.Roles.Operator),
            new Claim(PlatformConstants.ClaimTypes.ApiKey, "true")
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
