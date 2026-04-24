using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using POS.Backend.Common;
using POS.data.Data;
using POS.data.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace POS.Backend.Features.Auth
{
    public interface IAuthServices
    {
        Task<Result<AuthResponse>> LoginAsync(LoginRequest request);
        Task<Result<AuthResponse>> RefreshTokenAsync(string token);
        Task<Result<bool>> RevokeTokenAsync(string token);
    }

    public class AuthServices : IAuthServices
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly PasswordHasher<POS.data.Entities.User> _passwordHasher;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthServices(AppDbContext context, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _configuration = configuration;
            _passwordHasher = new PasswordHasher<POS.data.Entities.User>();
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request)
        {
            var user = await _context.Users
                .Include(u => u.Merchant)
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.Username == request.Username && u.DeletedAt == null);

            if (user == null)
                return Result<AuthResponse>.Failure("Invalid username or password.");

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (result == PasswordVerificationResult.Failed)
                return Result<AuthResponse>.Failure("Invalid username or password.");

            var jwtToken = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken(IpAddress());

            user.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            SetAuthCookies(jwtToken, refreshToken.Token, refreshToken.Expires);

            return Result<AuthResponse>.Success(new AuthResponse
            {
                Username = user.Username,
                Role = user.Role,
                UserId = user.Id,
                MerchantId = user.MerchantId,
                BranchId = user.BranchId
            });
        }

        public async Task<Result<AuthResponse>> RefreshTokenAsync(string token)
        {
            var user = await _context.Users
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == token));

            if (user == null) return Result<AuthResponse>.Failure("Invalid token.");

            var refreshToken = user.RefreshTokens.Single(x => x.Token == token);

            bool isActive = refreshToken.Revoked == null && !refreshToken.IsUsed && DateTime.UtcNow < refreshToken.Expires;
            if (!isActive) return Result<AuthResponse>.Failure("Token is not active.");

            var newRefreshToken = GenerateRefreshToken(IpAddress());
            refreshToken.Revoked = DateTime.UtcNow;
            refreshToken.RevokedByIp = IpAddress();
            refreshToken.ReplacedByToken = newRefreshToken.Token;
            refreshToken.IsUsed = true;

            user.RefreshTokens.Add(newRefreshToken);
            await _context.SaveChangesAsync();

            var jwtToken = GenerateJwtToken(user);
            SetAuthCookies(jwtToken, newRefreshToken.Token, newRefreshToken.Expires);

            return Result<AuthResponse>.Success(new AuthResponse
            {
                Username = user.Username,
                Role = user.Role,
                UserId = user.Id,
                MerchantId = user.MerchantId,
                BranchId = user.BranchId
            });
        }

        public async Task<Result<bool>> RevokeTokenAsync(string token)
        {
            var user = await _context.Users
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == token));

            if (user == null) return Result<bool>.Failure("Invalid token.");

            var refreshToken = user.RefreshTokens.Single(x => x.Token == token);

            bool isActive = refreshToken.Revoked == null && !refreshToken.IsUsed && DateTime.UtcNow < refreshToken.Expires;
            if (!isActive) return Result<bool>.Failure("Token is already inactive.");

            refreshToken.Revoked = DateTime.UtcNow;
            refreshToken.RevokedByIp = IpAddress();
            refreshToken.ReasonRevoked = "Revoked by user";

            await _context.SaveChangesAsync();

            ClearAuthCookies();

            return Result<bool>.Success(true);
        }

        private void SetAuthCookies(string jwtToken, string refreshToken, DateTime refreshExpires)
        {
            var http = _httpContextAccessor.HttpContext;
            if (http == null) return;

            var expiryMinutes = double.Parse(_configuration["Jwt:ExpiryInMinutes"] ?? "60");

            http.Response.Cookies.Append("access_token", jwtToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes)
            });

            http.Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = refreshExpires,
                Path = "/api/auth"   // Scope refresh token to auth endpoints only
            });
        }

        private void ClearAuthCookies()
        {
            var http = _httpContextAccessor.HttpContext;
            if (http == null) return;

            http.Response.Cookies.Delete("access_token");
            http.Response.Cookies.Delete("refresh_token", new CookieOptions { Path = "/api/auth" });
        }

        private string GenerateJwtToken(POS.data.Entities.User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("UserId", user.Id.ToString()),
                new Claim("MerchantId", user.MerchantId?.ToString() ?? ""),
                new Claim("BranchId", user.BranchId?.ToString() ?? "")
            };

            var expiryMinutes = double.Parse(_configuration["Jwt:ExpiryInMinutes"] ?? "60");

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private RefreshToken GenerateRefreshToken(string ipAddress)
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);

            return new RefreshToken
            {
                Token = Convert.ToBase64String(randomBytes),
                Expires = DateTime.UtcNow.AddDays(7),
                Created = DateTime.UtcNow,
                CreatedByIp = ipAddress
            };
        }

        private string IpAddress()
        {
            if (_httpContextAccessor.HttpContext?.Request.Headers.ContainsKey("X-Forwarded-For") == true)
                return _httpContextAccessor.HttpContext.Request.Headers["X-Forwarded-For"]!;
            else
                return _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "unknown";
        }
    }
}
