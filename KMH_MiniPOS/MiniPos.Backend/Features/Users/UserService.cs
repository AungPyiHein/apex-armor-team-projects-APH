using Common;
using Database.EfAppDbContextModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MiniPos.Backend.Features.Users;

public enum UserRole
{
    Admin,
    Merchant,
    Cashier
}

public interface IUserService
{
    Task<Result<PagedResult<UserListResponse>>> GetList(UserListRequest request);
    Task<Result<UserGetByIdResponse>> GetById(UserGetByIdRequest request);
    Task<Result<UserCreateResponse>> Create(UserCreateRequest request);
    Task<Result> Update(UserUpdateRequest request);
    Task<Result> ResetPassword(UserResetPasswordRequest request);
    Task<Result> AssignBranch(UserAssignBranchRequest request);
    Task<Result> Deactivate(UserDeactivateRequest request);
}

public class UserService : IUserService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<User> _passwordHasher;

    public UserService(AppDbContext db, IPasswordHasher<User> passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<PagedResult<UserListResponse>>> GetList(UserListRequest request)
    {
        var errCode = "User.List";
        try
        {
            if (request.IncludeCashiers)
            {
                var isOwner = await _db.Merchants
                    .AnyAsync(m =>
                        m.Users.Any(u => u.Id == request.ProcessedById && u.Role == nameof(UserRole.Merchant)));
                if (!isOwner)
                    return Result<PagedResult<UserListResponse>>.Failure(
                        new UnAuthorizedError(errCode, "UnAuthorized."));

                var query = _db.Users
                    .Where(u => u.MerchantId == request.MerchantId && u.Role == nameof(UserRole.Cashier))
                    .AsNoTracking()
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    var searchTerm = request.SearchTerm.Trim().ToLower();
                    query = query.Where(u =>
                        u.Username.ToLower().Contains(searchTerm) ||
                        u.Email.ToLower().Contains(searchTerm));
                }

                var totalCount = await query.CountAsync();
                var skip = (request.PageNumber - 1) * request.PageSize;
                var take = request.PageSize;

                var users = await query
                    .OrderByDescending(u => u.CreatedAt)
                    .Skip(skip)
                    .Take(take)
                    .Select(u => new UserListResponse
                    {
                        Id = u.Id,
                        UserName = u.Username,
                        Email = u.Email,
                        Role = u.Role,

                        Merchant = u.Merchant != null
                            ? new MerchantDto
                            {
                                Id = u.Merchant.Id,
                                Name = u.Merchant.Name
                            }
                            : null,

                        Branch = u.Branch != null
                            ? new BranchDto
                            {
                                Id = u.Branch.Id,
                                Name = u.Branch.Name
                            }
                            : null
                    })
                    .ToListAsync();

                var result =
                    new PagedResult<UserListResponse>(users, totalCount, request.PageNumber, request.PageSize);
                return Result<PagedResult<UserListResponse>>.Success(result);
            }

            return Result<PagedResult<UserListResponse>>.Failure(new NotFoundError(errCode, "Users not found."));
        }
        catch (Exception e)
        {
            return Result<PagedResult<UserListResponse>>.Failure(new InternalError("User.GetList", e.Message));
        }
    }

    public async Task<Result<UserGetByIdResponse>> GetById(UserGetByIdRequest request)
    {
        const string errCode = "User.GetById";
        try
        {
            var user = await _db.Users
                .AsNoTracking()
                .Select(u => new UserGetByIdResponse
                {
                    Id = u.Id,
                    UserName = u.Username,
                    Email = u.Email,
                    Role = u.Role,
                    JoinedDate = u.CreatedAt,
                    Merchant = u.Merchant != null
                        ? new MerchantDto
                        {
                            Id = u.Merchant.Id,
                            Name = u.Merchant.Name
                        }
                        : null,
                    Branch = u.Branch != null
                        ? new BranchDto
                        {
                            Id = u.Branch.Id,
                            Name = u.Branch.Name
                        }
                        : null,
                    TotalSales = u.Orders
                        .Sum(o => (decimal?)o.TotalAmount)
                        .ToString()
                })
                .FirstOrDefaultAsync(u => u.Id == request.UserId);

            if (user == null)
                return Result<UserGetByIdResponse>.Failure(new NotFoundError(errCode, "User does not exist"));

            return Result<UserGetByIdResponse>.Success(user);
        }
        catch (Exception e)
        {
            return Result<UserGetByIdResponse>.Failure(new InternalError(errCode, e.Message));
        }
    }

    public async Task<Result<UserCreateResponse>> Create(UserCreateRequest request)
    {
        const string errCode = "User.Create";
        try
        {
            var isExist = await _db.Users.AnyAsync(user => user.Email == request.Email);
            if (isExist)
                return Result<UserCreateResponse>.Failure(new ConflictError(errCode, "User already exist"));

            return request.Role switch
            {
                nameof(UserRole.Merchant) => await CreateMerchant(request),
                nameof(UserRole.Cashier) => await CreateCashier(request),
                _ => Result<UserCreateResponse>.Failure(new BadRequestError(errCode, "Invalid user role specified"))
            };
        }
        catch (Exception e)
        {
            return Result<UserCreateResponse>.Failure(new InternalError(errCode, e.Message));
        }
    }

    public async Task<Result> Update(UserUpdateRequest request)
    {
        const string errCode = "User.Update";
        try
        {
            var user = await _db.Users.FirstOrDefaultAsync(user => user.Id == request.UserId);
            if (user == null)
                return Result.Failure(new NotFoundError(errCode, "User does not exist"));

            user.Email = request.Email;
            user.Username = request.UserName;
            user.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Result.Success();
        }
        catch (Exception e)
        {
            return Result.Failure(new InternalError("User.Update", e.Message));
        }
    }

    public async Task<Result> ResetPassword(UserResetPasswordRequest request)
    {
        try
        {
            if (request.NewPassword != request.ConfirmPassword)
                return Result.Failure(new BadRequestError("User.UpdatePassword",
                    "New password and confirm password do not match"));

            var user = await _db.Users.FirstOrDefaultAsync(user => user.Id == request.UserId);
            if (user == null)
                return Result.Failure(new NotFoundError("User.UpdatePassword", "User does not exist"));

            var isValidPassword = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.OldPassword!);
            if (isValidPassword == PasswordVerificationResult.Failed)
                return Result.Failure(new BadRequestError("User.UpdatePassword", "Old password is incorrect"));

            var hashedPassword = _passwordHasher.HashPassword(user, request.NewPassword!);
            user.PasswordHash = hashedPassword;
            user.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception e)
        {
            return Result.Failure(new InternalError("User.UpdatePassword", e.Message));
        }
    }

    public async Task<Result> AssignBranch(UserAssignBranchRequest request)
    {
        const string errCode = "User.ReassignBranch";
        try
        {
            var isBranchExist = await _db.Branches.AnyAsync(b => b.Id == request.BranchId);
            if (!isBranchExist)
                return Result.Failure(new NotFoundError(errCode, "Branch does not exist"));

            var user = await _db.Users.FirstOrDefaultAsync(user => user.Id == request.UserId);
            if (user == null)
                return Result.Failure(new NotFoundError(errCode, "User does not exist"));

            user.BranchId = request.BranchId;
            user.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception e)
        {
            return Result.Failure(new InternalError(errCode, e.Message));
        }
    }

    public async Task<Result> Deactivate(UserDeactivateRequest request)
    {
        const string errCode = "User.Delete";
        try
        {
            var user = await _db.Users.FirstOrDefaultAsync(user => user.Id == request.UserId);
            if (user == null)
                return Result.Failure(new NotFoundError(errCode, "User does not exist"));

            user.DeletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception e)
        {
            return Result.Failure(new InternalError(errCode, e.Message));
        }
    }

    private async Task<Result<UserCreateResponse>> CreateMerchant(UserCreateRequest request)
    {
        const string errCode = "User.CreateMerchant";
        var isAdminExist =
            await _db.Users.AnyAsync(u => u.Id == request.ProcessedById && u.Role == nameof(UserRole.Admin));
        if (!isAdminExist)
            return Result<UserCreateResponse>.Failure(new NotFoundError("User.Create", "Admin does not exist"));

        var merchant = new Merchant
        {
            Name = request.MerchantName,
            ContactEmail = request.MerchantEmail,
            IsActive = true
        };
        await _db.Merchants.AddAsync(merchant);
        if (await _db.SaveChangesAsync() == 0)
            return Result<UserCreateResponse>.Failure(new InternalError(errCode,
                "Failed to create merchant for user"));

        var merchantAdmin = new User
        {
            Email = request.Email,
            Role = nameof(UserRole.Merchant),
            Username = request.UserName,
            MerchantId = merchant.Id
        };
        var passwordHash = _passwordHasher.HashPassword(merchantAdmin, request.Password!);
        merchantAdmin.PasswordHash = passwordHash;

        await _db.Users.AddAsync(merchantAdmin);
        var result = await _db.SaveChangesAsync();
        return result > 0
            ? Result<UserCreateResponse>.Success(new UserCreateResponse { Id = merchantAdmin.Id })
            : Result<UserCreateResponse>.Failure(new InternalError("User.Create", "Failed to create user"));
    }

    private async Task<Result<UserCreateResponse>> CreateCashier(UserCreateRequest request)
    {
        const string errCode = "User.CreateCashier";
        var isBranchExist = await _db.Branches
            .AsNoTracking()
            .Select(b => new { b.Id, MerchantId = b.Merchant.Id })
            .AnyAsync(b => b.Id == request.BranchId && b.MerchantId == request.MerchantId);
        if (!isBranchExist)
            return Result<UserCreateResponse>.Failure(new NotFoundError(errCode,
                "Branch does not exist with this merchant"));

        var user = new User
        {
            Email = request.Email,
            Role = nameof(UserRole.Cashier),
            Username = request.UserName,
            BranchId = request.BranchId,
            MerchantId = request.MerchantId
        };
        var passwordHash = _passwordHasher.HashPassword(user, request.Password!);
        user.PasswordHash = passwordHash;

        await _db.Users.AddAsync(user);
        var result = await _db.SaveChangesAsync();
        return result > 0
            ? Result<UserCreateResponse>.Success(new UserCreateResponse { Id = user.Id })
            : Result<UserCreateResponse>.Failure(new InternalError(errCode, "Failed to create cashier"));
    }

    private async Task<Result<UserGetByIdResponse>> GetMerchantById(Guid id)
    {
        var merchantAdmin = await _db.Users
            .AsNoTracking()
            .Select(u => new UserGetByIdResponse
            {
                Id = u.Id,
                Email = u.Email,
                Role = u.Role,
                UserName = u.Username
            })
            .FirstOrDefaultAsync(u => u.Id == id && u.Role == nameof(UserRole.Merchant));
        if (merchantAdmin is null)
            return Result<UserGetByIdResponse>.Failure(new NotFoundError("User.GetMerchantById",
                "Merchant admin does not exist"));

        return Result<UserGetByIdResponse>.Success(merchantAdmin);
    }

    private async Task<Result<UserGetByIdResponse>> GetCashierById(Guid userId)
    {
        var cashier = await _db.Users
            .AsNoTracking()
            .Select(u => new UserGetByIdResponse
            {
                Id = u.Id,
                UserName = u.Username,
                Email = u.Email,
                Role = u.Role,
                JoinedDate = u.CreatedAt,
                Merchant = u.Merchant != null
                    ? new MerchantDto
                    {
                        Id = u.Merchant.Id,
                        Name = u.Merchant.Name
                    }
                    : null,
                Branch = u.Branch != null
                    ? new BranchDto
                    {
                        Id = u.Branch.Id,
                        Name = u.Branch.Name
                    }
                    : null,
                TotalSales = u.Orders
                    .Where(o => o.DeletedAt == null)
                    .Sum(o => (decimal?)o.TotalAmount)
                    .ToString()
            })
            .FirstOrDefaultAsync(u => u.Id == userId && u.Role == nameof(UserRole.Cashier));

        return cashier == null
            ? Result<UserGetByIdResponse>.Failure(new NotFoundError("User.GetCashierById", "Cashier does not exist"))
            : Result<UserGetByIdResponse>.Success(cashier);
    }
}

public class MerchantDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? ContactEmail { get; set; }
}

public class BranchDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }
}

public class UserListRequest : PaginationFilter
{
    public string? SearchTerm { get; set; }
    public bool IncludeCashiers { get; set; } = false;
    public bool IncludeMerchants { get; set; } = false;
    public Guid ProcessedById { get; set; }
    public Guid MerchantId { get; set; }
}

public class UserListResponse
{
    public Guid Id { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
    public MerchantDto? Merchant { get; set; }
    public BranchDto? Branch { get; set; }
}

public class UserGetByIdRequest
{
    public Guid UserId { get; set; }
    public Guid ProcessedById { get; set; }
}

public class UserGetByIdResponse
{
    public Guid Id { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
    public string? TotalSales { get; set; }
    public MerchantDto? Merchant { get; set; }
    public BranchDto? Branch { get; set; }
    public DateTime? JoinedDate { get; set; }
}

public class UserCreateRequest
{
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
    public string? Password { get; set; }
    public string? MerchantName { get; set; }
    public string? MerchantEmail { get; set; }
    public Guid MerchantId { get; set; }
    public Guid BranchId { get; set; }
    public Guid ProcessedById { get; set; }
}

public class UserCreateResponse
{
    public Guid Id { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
}

public class UserUpdateRequest
{
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public Guid UserId { get; set; }
    public Guid ProcessedById { get; set; }
}

public class UserResetPasswordRequest
{
    public string? OldPassword { get; set; }
    public string? NewPassword { get; set; }
    public string? ConfirmPassword { get; set; }
    public Guid UserId { get; set; }
    public Guid ProcessedById { get; set; }
}

public class UserAssignBranchRequest
{
    public Guid BranchId { get; set; }
    public Guid UserId { get; set; }
    public Guid ProcessedById { get; set; }
}

public class UserDeactivateRequest
{
    public Guid UserId { get; set; }
    public Guid ProcessedById { get; set; }
}