using Common;
using Database.EfAppDbContextModels;
using Microsoft.EntityFrameworkCore;
using MiniPos.Backend.Features.Users;

namespace MiniPos.Backend.Features.Merchants;

public interface IMerchantService
{
    Task<Result<PagedResult<MerchantListResponse>>> GetList(MerchantListRequest request);
    Task<Result<MerchantGetByIdResponse>> GetById(MerchantGetByIdRequest request);
    Task<Result<MerchantCreateResponse>> Create(MerchantCreateRequest request);
    Task<Result> Update(MerchantUpdateRequest request);
    Task<Result> Delete(MerchantDeleteRequest request);
}

public class MerchantService : IMerchantService
{
    private readonly AppDbContext _db;

    public MerchantService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PagedResult<MerchantListResponse>>> GetList(MerchantListRequest request)
    {
        try
        {
            var query = _db.MerchantAdmins
                .Where(m => m.UserId == request.MerchantAdminId)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                query = query.Where(m => m.Merchant.Name.Contains(request.SearchTerm));

            var skip = (request.PageNumber - 1) * request.PageSize;
            var take = request.PageSize;
            var totalCount = await query.CountAsync();
            var merchants = await query
                .Skip(skip)
                .Take(take)
                .Select(m => new MerchantListResponse
                {
                    Id = m.Merchant.Id,
                    Name = m.Merchant.Name,
                    ContactEmail = m.Merchant.ContactEmail,
                    BranchCount = m.Merchant.Branches.Count
                })
                .ToListAsync();

            var result =
                new PagedResult<MerchantListResponse>(merchants, totalCount, request.PageNumber, request.PageSize);
            return Result<PagedResult<MerchantListResponse>>.Success(result);
        }
        catch (Exception e)
        {
            return Result<PagedResult<MerchantListResponse>>.Failure(new InternalError("Merchant.GetList", e.Message));
        }
    }

    public async Task<Result<MerchantGetByIdResponse>> GetById(MerchantGetByIdRequest request)
    {
        try
        {
            var merchant = await _db.MerchantAdmins
                .AsNoTracking()
                .Where(m => m.UserId == request.MerchantAdminId && m.Merchant.Id == request.MerchantId)
                .Select(m => new MerchantGetByIdResponse
                {
                    Id = m.Merchant.Id,
                    Name = m.Merchant.Name,
                    ContactEmail = m.Merchant.ContactEmail,
                    IsActive = m.Merchant.IsActive,
                    CreatedAt = m.Merchant.CreatedAt,
                    BranchCount = m.Merchant.Branches.Count,
                    EmployeeCount = m.Merchant.Users.Count(u => u.Role == nameof(UserRole.Cashier)),
                    ProductCount = m.Merchant.Products.Count
                })
                .FirstOrDefaultAsync();

            if (merchant == null)
                return Result<MerchantGetByIdResponse>.Failure(new NotFoundError("Merchant.GetById",
                    "Merchant not found"));

            return Result<MerchantGetByIdResponse>.Success(merchant);
        }
        catch (Exception e)
        {
            return Result<MerchantGetByIdResponse>.Failure(new InternalError("Merchant.GetById", e.Message));
        }
    }

    public async Task<Result<MerchantCreateResponse>> Create(MerchantCreateRequest request)
    {
        const string errCode = "Merchant.Create";
        try
        {
            var merchantId = Guid.NewGuid();
            var merchant = new Merchant
            {
                Id = merchantId,
                Name = request.Name,
                ContactEmail = request.ContactEmail,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var merchantAdmin = new MerchantAdmin
            {
                MerchantId = merchantId,
                UserId = request.MerchantAdminId,
                CreatedAt = DateTime.UtcNow
            };

            await _db.Merchants.AddAsync(merchant);
            await _db.MerchantAdmins.AddAsync(merchantAdmin);
            await _db.SaveChangesAsync();

            return Result<MerchantCreateResponse>.Success(new MerchantCreateResponse { Id = merchantId });
        }
        catch (Exception e)
        {
            return Result<MerchantCreateResponse>.Failure(new InternalError("", e.Message));
        }
    }

    public async Task<Result> Update(MerchantUpdateRequest request)
    {
        const string errCode = "Merchant.Update";
        try
        {
            if (!await IsMerchantOwner(request.MerchantAdminId, request.MerchantId))
                return Result.Failure(new UnAuthorizedError(errCode, "You are not authorized to update this merchant"));

            var merchant = await _db.Merchants.FirstOrDefaultAsync(m => m.Id == request.MerchantId);
            if (merchant == null)
                return Result.Failure(new NotFoundError(errCode, "Merchant not found"));

            merchant.Name = request.Name;
            merchant.ContactEmail = request.ContactEmail;
            merchant.IsActive = request.IsActive;
            merchant.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Result.Success();
        }
        catch (Exception e)
        {
            return Result.Failure(new InternalError(errCode, e.Message));
        }
    }

    public async Task<Result> Delete(MerchantDeleteRequest request)
    {
        const string errCode = "Merchant.Delete";
        try
        {
            var merchantAdmin = await _db.MerchantAdmins.FirstOrDefaultAsync(m =>
                m.UserId == request.MerchantAdminId && m.MerchantId == request.MerchantId);
            if (merchantAdmin == null)
                return Result.Failure(new UnAuthorizedError(errCode, "You are not authorized to delete this merchant"));

            var merchant = await _db.Merchants
                .FirstOrDefaultAsync(m => m.Id == request.MerchantId);
            if (merchant == null)
                return Result.Failure(new NotFoundError(errCode, "Merchant not found"));

            merchantAdmin.DeletedAt = DateTime.UtcNow;
            merchant.DeletedAt = DateTime.UtcNow;
            merchant.IsActive = false;

            await _db.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception e)
        {
            return Result.Failure(new InternalError(errCode, e.Message));
        }
    }

    private async Task<bool> IsMerchantOwner(Guid merchantAdminId, Guid merchantId)
    {
        return await _db.MerchantAdmins.AnyAsync(m => m.UserId == merchantAdminId && m.MerchantId == merchantId);
    }
}

public class MerchantListRequest : PaginationFilter
{
    public Guid MerchantAdminId { get; set; }
    public string? SearchTerm { get; set; }
}

public class MerchantListResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string ContactEmail { get; set; } = "";
    public int BranchCount { get; set; }
}

public class MerchantGetByIdRequest
{
    public Guid MerchantAdminId { get; set; }
    public Guid MerchantId { get; set; }
}

public class MerchantGetByIdResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string ContactEmail { get; set; } = "";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int BranchCount { get; set; }
    public int ProductCount { get; set; }
    public int EmployeeCount { get; set; }
}

public class MerchantCreateRequest
{
    public string Name { get; set; } = "";
    public string ContactEmail { get; set; } = "";
    public Guid MerchantAdminId { get; set; }
}

public class MerchantCreateResponse
{
    public Guid Id { get; set; }
}

public class MerchantUpdateRequest
{
    public string Name { get; set; } = "";
    public string ContactEmail { get; set; } = "";
    public bool IsActive { get; set; }
    public Guid MerchantAdminId { get; set; }
    public Guid MerchantId { get; set; }
}

public class MerchantDeleteRequest
{
    public Guid MerchantAdminId { get; set; }
    public Guid MerchantId { get; set; }
}