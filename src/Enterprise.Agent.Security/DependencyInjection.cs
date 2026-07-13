using System.Text;
using Enterprise.Agent.Security.ApiKey;
using Enterprise.Agent.Security.Jwt;
using Enterprise.Agent.Security.Options;
using Enterprise.Agent.Shared.Constants;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Enterprise.Agent.Security;

/// <summary>Registers JWT + API-key authentication and the platform authorization policies.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddPlatformSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<ApiKeyOptions>(configuration.GetSection(ApiKeyOptions.SectionName));

        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IApiKeyValidator, ApiKeyValidator>();

        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        // A signing key is required by the validation parameters; use a development fallback when unset.
        var signingKey = string.IsNullOrWhiteSpace(jwt.SigningKey)
            ? "dev-only-insecure-signing-key-change-me-please-1234567890"
            : jwt.SigningKey;

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            })
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                ApiKeyAuthenticationHandler.SchemeName, _ => { });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(PlatformConstants.Policies.RequireUser, policy =>
                policy.RequireAuthenticatedUser());

            options.AddPolicy(PlatformConstants.Policies.RequireAdmin, policy =>
                policy.RequireRole(PlatformConstants.Roles.Admin));

            options.AddPolicy(PlatformConstants.Policies.RequireAgentOperator, policy =>
                policy.RequireRole(PlatformConstants.Roles.Admin, PlatformConstants.Roles.Operator));

            options.AddPolicy(PlatformConstants.Policies.ApiKeyOrJwt, policy =>
            {
                policy.AddAuthenticationSchemes(
                    JwtBearerDefaults.AuthenticationScheme, ApiKeyAuthenticationHandler.SchemeName);
                policy.RequireAuthenticatedUser();
            });
        });

        return services;
    }
}
