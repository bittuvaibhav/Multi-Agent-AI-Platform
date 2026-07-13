using Enterprise.Agent.Api.Contracts;
using Enterprise.Agent.Security.Jwt;
using Enterprise.Agent.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Enterprise.Agent.Api.Controllers;

/// <summary>Issues access tokens. In production this would delegate to an identity provider.</summary>
[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly ITokenService _tokenService;

    public AuthController(ITokenService tokenService) => _tokenService = tokenService;

    /// <summary>Issues a signed JWT for the given subject and roles.</summary>
    [HttpPost("token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    public ActionResult<TokenResponse> IssueToken([FromBody] IssueTokenRequest request)
    {
        var roles = request.Roles is { Length: > 0 } ? request.Roles : [PlatformConstants.Roles.User];
        var token = _tokenService.CreateToken(new TokenRequest(request.Subject, roles, request.TenantId));
        return Ok(token);
    }
}
