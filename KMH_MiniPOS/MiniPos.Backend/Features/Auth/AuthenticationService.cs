using Common;
using Database.EfAppDbContextModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MiniPos.Backend.Features.Users;

namespace MiniPos.Backend.Features.Auth;

public interface IAuthenticationService
{
    Task<Result<SignupResponse>> Signup(SignupRequest request);
    Task<Result<SigninResponse>> Signin(SigninRequest request);
    Task<Result<RefreshResponse>> Refresh(RefreshRequest request);
    Task<Result> Signout(Guid userId);
}

public class AuthenticationService : IAuthenticationService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly TokenService _tokenService;
    private readonly IUserService _userService;
    // private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthenticationService(
        AppDbContext db,
        IPasswordHasher<User> passwordHasher,
        IUserService userService,
        TokenService tokenService
        // IHttpContextAccessor httpContextAccessor
    )
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _userService = userService;
        _tokenService = tokenService;
        // _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<SignupResponse>> Signup(SignupRequest request)
    {
        var result = await _userService.Create(new UserCreateRequest
        {
            UserName = request.UserName,
            Email = request.Email,
            Role = request.Role,
            Password = request.Password,
            MerchantName = request.MerchantName,
            MerchantEmail = request.MerchantEmail,
            MerchantId = request.MerchantId,
            ProcessedById = request.ProcessedById,
            BranchId = request.BranchId
        });

        if (result is { IsSuccess: false, Data: not null }) return Result<SignupResponse>.Failure(result.Error!);

        var user = result.Data;
        var token = await _tokenService.IssueTokenAsync(new IssueTokenRequest
        {
            Role = user.Role,
            UserId = user.Id,
        });
       
        var response = new SignupResponse
        {
            UserId = user.Id,
            Role = user.Role,
            Token = token.Data
        };

        return Result<SignupResponse>.Success(response);
    }

    public async Task<Result<SigninResponse>> Signin(SigninRequest request)
    {
        var errCode = "AuthenticationService.Signin";
        try
        {
            Console.WriteLine($"Signin attempt for email: {request.Email}");
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user is null) return Result<SigninResponse>.Failure(new UnAuthorizedError(errCode, "Invalid credentials"));

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (result == PasswordVerificationResult.Failed)
                return Result<SigninResponse>.Failure(new UnAuthorizedError(errCode, "Invalid credentials"));

            var token = await _tokenService.IssueTokenAsync(new IssueTokenRequest
            {
                Role = user.Role,
                UserId = user.Id,
            });
            return Result<SigninResponse>.Success(new SigninResponse
            {
                UserId = user.Id,
                Role = user.Role,
                Token = token.Data
            });
        }
        catch (Exception e)
        {
            return Result<SigninResponse>.Failure(new InternalError(errCode, e.Message));
        }
    }

    public async Task<Result<RefreshResponse>> Refresh(RefreshRequest request)
    {
        return await _tokenService.RefreshAsync(request.RefreshToken);
    }

    public async Task<Result> Signout(Guid userid)
    {
        try
        {
            var refreshToken = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.UserId == userid);
            if (refreshToken == null)
                return Result.Failure(new NotFoundError("AuthenticationService.Signout", "Refresh token not found."));

            refreshToken.Token = "";
            refreshToken.IsRevoked = true;
            refreshToken.UpdatedAt = DateTime.UtcNow;
            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.ReplacedByTokenHash = null;
            refreshToken.ExpiresAt = null;
            
            await _db.SaveChangesAsync();
            return Result.Success();
        }
        catch (Exception e)
        {
            return Result.Failure(new InternalError("AuthenticationService.Signout", e.Message));
        }
    }
}

public class SignupRequest
{
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
    public string? Password { get; set; }
    public string? MerchantName { get; set; }
    public string? MerchantEmail { get; set; }
    public Guid MerchantId { get; set; }
    public Guid ProcessedById { get; set; }
    public Guid BranchId { get; set; }
}

public class SignupResponse
{
    public Guid UserId { get; set; }
    public string? Role { get; set; }
    public TokenResponse? Token { get; set; }
}

public class SigninRequest
{
    public string? Email { get; set; }
    public string? Password { get; set; }
}

public class SigninResponse
{
    public Guid UserId { get; set; }
    public string? Role { get; set; }
    public TokenResponse? Token { get; set; }
}

public class SignoutResponse
{
    public string Message { get; set; } = string.Empty;
}

public class RefreshResponse
{
    public Guid UserId { get; set; }
    public string? Role { get; set; }
    public TokenResponse? Token { get; set; }
}

public class RefreshRequest
{
    public string RefreshToken { get; set; } = "";
}