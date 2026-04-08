using Microsoft.AspNetCore.Mvc;
using POS.Shared.Models;

namespace POS.Backend.Features.Auth
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthServices _authServices;

        public AuthController(IAuthServices authServices)
        {
            _authServices = authServices;
        }

        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _authServices.LoginAsync(request);
            if (!result.IsSuccess)
                return Unauthorized(result.Error);

            return Ok(result.Value);
        }

        [HttpPost("refresh-token")]
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public async Task<IActionResult> RefreshToken()
        {
            var token = Request.Cookies["refresh_token"];
            if (string.IsNullOrEmpty(token))
                return BadRequest("Refresh token not found.");

            var result = await _authServices.RefreshTokenAsync(token);
            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Value);
        }

        [HttpPost("revoke-token")]
        public async Task<IActionResult> RevokeToken()
        {
            var token = Request.Cookies["refresh_token"];
            if (string.IsNullOrEmpty(token))
                return BadRequest("Refresh token not found.");

            var result = await _authServices.RevokeTokenAsync(token);
            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(new { Message = "Token revoked successfully." });
        }

        [HttpGet("me")]
        public IActionResult Me()
        {
            var user = HttpContext.User;
            if (user?.Identity?.IsAuthenticated != true)
                return Unauthorized();

            return Ok(new AuthResponse
            {
                Username = user.FindFirst("sub")?.Value ?? user.Identity.Name ?? "",
                Role = user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "",
                UserId = Guid.TryParse(user.FindFirst("UserId")?.Value, out var uid) ? uid : null,
                MerchantId = Guid.TryParse(user.FindFirst("MerchantId")?.Value, out var mid) ? mid : null,
                BranchId = Guid.TryParse(user.FindFirst("BranchId")?.Value, out var bid) ? bid : null
            });
        }
    }
}
