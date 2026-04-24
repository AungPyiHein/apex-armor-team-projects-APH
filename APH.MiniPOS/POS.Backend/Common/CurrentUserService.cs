using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using POS.Shared.Models;
using System.IdentityModel.Tokens.Jwt;

namespace POS.Backend.Common;

public interface ICurrentUserService
{
    Guid UserId { get; }
    string Username { get; }
    UserRole Role { get; }
    Guid? MerchantId { get; }
    Guid? BranchId { get; }
    bool IsAuthenticated { get; }
}

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid UserId
    {
        get
        {
            var id = User?.FindFirst("UserId")?.Value;
            return Guid.TryParse(id, out var result) ? result : Guid.Empty;
        }
    }

    public string Username => User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                              ?? string.Empty;

    public UserRole Role
    {
        get
        {
            var roleStr = User?.FindFirst(ClaimTypes.Role)?.Value ?? User?.FindFirst("role")?.Value;
            return Enum.TryParse<UserRole>(roleStr, true, out var role) ? role : UserRole.Staff;
        }
    }

    public Guid? MerchantId
    {
        get
        {
            var idString = User?.FindFirst("MerchantId")?.Value ?? User?.FindFirst("merchantId")?.Value;
            return string.IsNullOrWhiteSpace(idString) ? null : Guid.Parse(idString);
        }
    }

    public Guid? BranchId
    {
        get
        {
            var idString = User?.FindFirst("BranchId")?.Value;
            return string.IsNullOrWhiteSpace(idString) ? null : Guid.Parse(idString);
        }
    }

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
}
