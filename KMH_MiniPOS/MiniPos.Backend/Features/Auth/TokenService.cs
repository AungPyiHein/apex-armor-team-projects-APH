using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Common;
using Database.EfAppDbContextModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace MiniPos.Backend.Features.Auth;

public class TokenService
{
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _db;

    public TokenService(IConfiguration configuration, AppDbContext db)
    {
        _configuration = configuration;
        _db = db;
    }

    public async Task<Result<TokenResponse>> IssueTokenAsync(IssueTokenRequest request)
    {
        var accessMinutes = int.Parse(_configuration["Jwt:AccessTokenMinutes"]!);
        var refreshDays = int.Parse(_configuration["Jwt:RefreshTokenDays"]!);
        var now = DateTime.Now;
        var accessExpires = now.AddMinutes(accessMinutes);
        var refreshExpires = now.AddDays(refreshDays);
        var refreshToken = GenerateSecureToken();
        var refreshHash = HashToken(refreshToken);
        var extraClaims = new List<Claim>
        {
            new(ClaimTypes.Role, request.Role),
        };
        var accessToken = CreateAccessToken(request.UserId, accessExpires, extraClaims);

        try
        {
            var existingRefreshToken = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.UserId == request.UserId && !t.IsRevoked);
            if (existingRefreshToken != null)
            {
                existingRefreshToken.Token = refreshHash;
                existingRefreshToken.ExpiresAt = refreshExpires;
                existingRefreshToken.ReplacedByTokenHash = null;
                existingRefreshToken.UpdatedAt = now;
            }
            else
            {
                var refreshTokenEntity = new RefreshToken
                {
                    Token = refreshHash,
                    UserId = request.UserId,
                    IsRevoked = false,
                    ExpiresAt = refreshExpires,
                    CreatedAt = now,
                };

                await _db.RefreshTokens.AddAsync(refreshTokenEntity);
            }

            await _db.SaveChangesAsync();

            var rep = new TokenResponse
            {
                AccessToken = accessToken,
                AccessTokenExpiresAtUtc = accessExpires,
                RefreshToken = refreshHash,
                RefreshTokenExpiresAtUtc = refreshExpires
            };

            return Result<TokenResponse>.Success(rep);
        }
        catch (Exception e)
        {
            return Result<TokenResponse>.Failure(new InternalError("TokenService.IssueTokenAsync", e.Message));
        }
    }

    public async Task<Result<RefreshResponse>> RefreshAsync(string token)
    {
        try
        {
            var refreshToken = await _db.RefreshTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == token);

            if (refreshToken == null)
                return Result<RefreshResponse>.Failure(new UnAuthorizedError("TokenService.RefreshAsync",
                    "Refresh is expired."));

            var now = DateTimeOffset.UtcNow;
            if (refreshToken.IsRevoked || refreshToken.ExpiresAt == null)
                return Result<RefreshResponse>.Failure(new UnAuthorizedError("TokenService.RefreshAsync",
                    "Refresh is expired."));

            var accessMinutes = int.Parse(_configuration["Jwt:AccessTokenMinutes"]!);
            var refreshDays = int.Parse(_configuration["Jwt:RefreshTokenDays"]!);
            var accessExpires = now.AddMinutes(accessMinutes);
            var refreshExpires = now.AddDays(refreshDays);

            var extraClaims = new List<Claim>
            {
                new(ClaimTypes.Role, refreshToken.User.Role),
            };
            var accessToken = CreateAccessToken(refreshToken.UserId, accessExpires, extraClaims);

            var shouldRotateRefreshToken = refreshToken.ExpiresAt <= now.AddDays(1);
            if (shouldRotateRefreshToken)
            {
                var refreshStr = GenerateSecureToken();
                var refreshHash = HashToken(refreshStr);

                refreshToken.Token = refreshHash;
                refreshToken.ExpiresAt = refreshExpires;
                refreshToken.ReplacedByTokenHash = refreshHash;
                refreshToken.UpdatedAt = now.UtcDateTime;
            }
            else
            {
                refreshToken.UpdatedAt = now.UtcDateTime;
            }

            await _db.SaveChangesAsync();

            var tokenResponse = new TokenResponse
            {
                AccessToken = accessToken,
                AccessTokenExpiresAtUtc = accessExpires,
                RefreshToken = refreshToken.Token,
                RefreshTokenExpiresAtUtc = refreshToken.ExpiresAt!.Value
            };

            return Result<RefreshResponse>.Success(new RefreshResponse
            {
                UserId = refreshToken.UserId,
                Role = refreshToken.User.Role,
                Token = tokenResponse,
            });
        }
        catch (Exception e)
        {
            return Result<RefreshResponse>.Failure(new InternalError("TokenService.RefreshAsync", e.Message));
        }
    }

    private string CreateAccessToken(Guid userId, DateTimeOffset expiresAtUtc, IEnumerable<Claim>? extraClaims)
    {
        var issuer = _configuration["Jwt:Issuer"]!;
        var audience = _configuration["Jwt:Audience"]!;
        var key = _configuration["Jwt:Key"]!;
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(ClaimTypes.Name, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (extraClaims != null)
            claims.AddRange(extraClaims);

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            DateTime.UtcNow,
            expiresAtUtc.UtcDateTime,
            creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToHexString(bytes);
    }

    private static string HashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}

public class TokenResponse
{
    public string AccessToken { get; set; } = "";
    public DateTimeOffset AccessTokenExpiresAtUtc { get; set; }

    public string RefreshToken { get; set; } = "";
    public DateTimeOffset RefreshTokenExpiresAtUtc { get; set; }
}

public class IssueTokenRequest
{
    public required Guid UserId { get; set; }
    public required string Role { get; set; } = string.Empty;
}