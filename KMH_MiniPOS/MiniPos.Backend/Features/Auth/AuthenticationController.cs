using Mapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MiniPos.Backend.Extensions;

namespace MiniPos.Backend.Features.Auth;

[ApiController]
[Route("api/auth")]
public class AuthenticationController : ControllerBase
{
    private readonly IAuthenticationService _authService;
    private const string XAccessToken = "X-Access-Token";
    private const string XRefreshToken = "X-Refresh-Token";
    private const string XUserId = "X-User-Id";
    private const string XRole = "X-Role";

    public AuthenticationController(IAuthenticationService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("signup")]
    public async Task<IActionResult> Signup([FromBody] SignupRequest request)
    {
        var processedById = User.GetUserId();
        request.ProcessedById = processedById;

        var result = await _authService.Signup(request);
        if (result is { IsSuccess: true, Data: { Token: not null, Role: not null } })
        {
            SetAuthCookies(result.Data.Token, result.Data.UserId, result.Data.Role);
            return Created($"api/users/{result.Data!.UserId}", result.Data);
        }

        var statusCode = ErrorHttpMapper.GetStatusCode(result.Error!);
        return StatusCode(statusCode, new { message = result.Error!.Message });
    }

    [AllowAnonymous]
    [HttpPost("signin")]
    public async Task<IActionResult> Signin([FromBody] SigninRequest request)
    {
        var result = await _authService.Signin(request);
        if (result is { IsSuccess: true, Data: { Token: not null, Role: not null } })
        {
            SetAuthCookies(result.Data!.Token, result.Data.UserId, result.Data.Role);
            return Ok(result.Data.Token);
        }

        var statusCode = ErrorHttpMapper.GetStatusCode(result.Error!);
        return StatusCode(statusCode, new { message = result.Error!.Message });
    }

    [Authorize]
    [HttpGet("signout")]
    public async Task<IActionResult> Signout()
    {
        var userId = User.GetUserId();
        var result = await _authService.Signout(userId);
        if (!result.IsSuccess)
        {
            var statusCode = ErrorHttpMapper.GetStatusCode(result.Error!);
            return StatusCode(statusCode, new { message = result.Error!.Message });
        }

        Response.Cookies.Delete(XAccessToken);
        Response.Cookies.Delete(XRefreshToken);
        Response.Cookies.Delete(XUserId);
        Response.Cookies.Delete(XRole);
        return Ok(new { message = "Logged out successfully" });
    }

    [Authorize]
    [HttpGet("refresh")]
    public async Task<IActionResult> Refresh()
    {
        if (!Request.Cookies.TryGetValue(XRefreshToken, out var refreshToken))
        {
            return Unauthorized("Refresh token missing");
        }

        var result = await _authService.Refresh(new RefreshRequest
        {
            RefreshToken = refreshToken
        });

        if (result is { IsSuccess: true, Data: { Token: not null, Role: not null } })
        {
            SetAuthCookies(result.Data.Token, result.Data.UserId, result.Data.Role);
            return Ok();
        }

        var statusCode = ErrorHttpMapper.GetStatusCode(result.Error!);
        return StatusCode(statusCode, new { message = result.Error!.Message });
    }

    [Authorize]
    [HttpGet("check")]
    public async Task<IActionResult> Check()
    {
        return Ok();
    }

    private void SetAuthCookies(TokenResponse tokenResponse, Guid userId, string role)
    {
        var accessCookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = tokenResponse.AccessTokenExpiresAtUtc
        };

        var refreshCookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = tokenResponse.RefreshTokenExpiresAtUtc
        };

        Response.Cookies.Append(XAccessToken, tokenResponse.AccessToken, accessCookieOptions);
        Response.Cookies.Append(XRefreshToken, tokenResponse.RefreshToken, refreshCookieOptions);
        Response.Cookies.Append(XUserId, userId.ToString(), new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = tokenResponse.AccessTokenExpiresAtUtc
        });
        Response.Cookies.Append(XRole, role, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = tokenResponse.AccessTokenExpiresAtUtc
        });
    }
}