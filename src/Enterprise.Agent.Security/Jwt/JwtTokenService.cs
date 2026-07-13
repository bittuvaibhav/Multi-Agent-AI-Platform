using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Enterprise.Agent.Security.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Enterprise.Agent.Security.Jwt;

/// <summary>A request to issue a token for a subject with roles.</summary>
public sealed record TokenRequest(string Subject, IReadOnlyList<string> Roles, string? TenantId = null);

/// <summary>An issued access token.</summary>
public sealed record TokenResponse(string AccessToken, DateTimeOffset ExpiresAt, string TokenType = "Bearer");

/// <summary>Issues signed JWT access tokens.</summary>
public interface ITokenService
{
    TokenResponse CreateToken(TokenRequest request);
}

/// <summary>HMAC-SHA256 JWT token service.</summary>
public sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options) => _options = options.Value;

    public TokenResponse CreateToken(TokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(_options.SigningKey))
        {
            throw new InvalidOperationException("JWT signing key is not configured.");
        }

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_options.ExpiryMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, request.Subject),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };
        claims.AddRange(request.Roles.Select(r => new Claim(ClaimTypes.Role, r)));
        if (!string.IsNullOrWhiteSpace(request.TenantId))
        {
            claims.Add(new Claim("tenant_id", request.TenantId!));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        return new TokenResponse(accessToken, expiresAt);
    }
}
