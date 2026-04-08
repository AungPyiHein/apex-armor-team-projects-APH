using Common;
using Database.EfAppDbContextModels;
using Microsoft.EntityFrameworkCore;
using MiniPos.Backend.Features.Users;

namespace MiniPos.Backend.Features.Profile;

public interface IProfileService
{
    Task<Result<ProfileResponse>> GetProfile(Guid userId);
}

public class ProfileService : IProfileService
{
    private readonly AppDbContext _db;

    public ProfileService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<ProfileResponse>> GetProfile(Guid userId)
    {
        Console.WriteLine($"userid is {userId}");
        try
        {
            var profile = await _db.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new ProfileResponse
                {
                    Id = u.Id,
                    UserName = u.Username,
                    Email = u.Email,
                    Role = u.Role,
                    CreatedAt = u.CreatedAt,
                    Merchant = u.Merchant != null ? new ProfileMerchantDto
                    {
                        Id = u.Merchant.Id,
                        Name = u.Merchant.Name,
                        ContactEmail = u.Merchant.ContactEmail
                    } : null,
                    Branch = u.Branch != null ? new ProfileBranchDto
                    {
                        Id = u.Branch.Id,
                        Name = u.Branch.Name,
                        Address = u.Branch.Address
                    } : null,
                    TotalSales = u.Orders.Where(o => o.DeletedAt == null).Sum(o => (decimal?)o.TotalAmount).GetValueOrDefault()
                })
                .FirstOrDefaultAsync();

            if (profile == null)
                return Result<ProfileResponse>.Failure(new NotFoundError("Profile.Get", "User profile not found."));

            return Result<ProfileResponse>.Success(profile);
        }
        catch (Exception ex)
        {
            return Result<ProfileResponse>.Failure(new InternalError("Profile.Get", ex.Message));
        }
    }
}

public class ProfileResponse
{
    public Guid Id { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal TotalSales { get; set; }
    public ProfileMerchantDto? Merchant { get; set; }
    public ProfileBranchDto? Branch { get; set; }
}

public class ProfileMerchantDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? ContactEmail { get; set; }
}

public class ProfileBranchDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Address { get; set; }
}

public class UpdateProfileRequest
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public Guid ProcessedById { get; set; }
}

public class UpdateProfilePasswordRequest
{
    public string? OldPassword { get; set; }
    public string? NewPassword { get; set; }
    public string? ConfirmPassword { get; set; }
    public Guid ProcessedById { get; set; }
}
