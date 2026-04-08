using Common;
using Database.EfAppDbContextModels;
using Microsoft.EntityFrameworkCore;

namespace MiniPos.Backend.Features.Branches;

public interface IBranchService
{
    Task<Result<PagedResult<BranchListResponse>>> GetList(BranchListRequest request);
    Task<Result<BranchGetByIdResponse>> GetById(Guid id);
    Task<Result> Create(BranchCreateRequest request);
    Task<Result> Update(Guid branchId, BranchUpdateRequest request);
    Task<Result> Delete(Guid id);
}

public class BranchService : IBranchService
{
    private readonly AppDbContext _db;

    public BranchService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PagedResult<BranchListResponse>>> GetList(BranchListRequest request)
    {
        try
        {
            var query = _db.Branches
                .Where(b => b.MerchantId ==request.MerchantId)
                .AsNoTracking()
                .AsQueryable();
            
            var skip = (request.PageNumber - 1) * request.PageSize;
            var take = request.PageSize;
            var totalCount = await query.CountAsync();
            
            query = query.Skip(skip).Take(take);
            List<BranchListResponse> items;
            PagedResult<BranchListResponse> result;
            
            if (request.IncludeMerchant)
            {
                items = await query
                    .Select(b => new BranchListResponse
                    {
                        Id = b.Id,
                        Name = b.Name,
                        Address = b.Address,
                        Merchant = new MerchantDto
                        {
                            Id = b.Merchant.Id,
                            Name = b.Merchant.Name,
                            ContactEmail = b.Merchant.ContactEmail
                        }
                    })
                    .ToListAsync();
                
                result =
                    new PagedResult<BranchListResponse>(items, totalCount, request.PageNumber, request.PageSize);
                return Result<PagedResult<BranchListResponse>>.Success(result);
            }
            
            items = await query
                .Select(b => new BranchListResponse
                {
                    Id = b.Id,
                    Name = b.Name,
                })
                .ToListAsync();

            result =
                new PagedResult<BranchListResponse>(items, totalCount, request.PageNumber, request.PageSize);
            return Result<PagedResult<BranchListResponse>>.Success(result);
        }
        catch (Exception e)
        {
            return Result<PagedResult<BranchListResponse>>.Failure(new InternalError("Branch.GetList", e.Message));
        }
    }

    public async Task<Result<BranchGetByIdResponse>> GetById(Guid branchId)
    {
        const string errCode = "Branch.GetById";
        try
        {
            var todayStart = DateTime.Today; 
            var tomorrowStart = todayStart.AddDays(1);

            var branchData = await _db.Branches
                .AsNoTracking()
                .Where(b => b.Id == branchId && b.DeletedAt == null)
                .Select(b => new
                {
                    b.Id,
                    b.Name,
                    b.Address,
                    Merchant = new MerchantDto
                    {
                        Id = b.Merchant.Id,
                        Name = b.Merchant.Name
                    },
                    TodayOrderCount = b.Orders.Count(o => o.CreatedAt >= todayStart && o.CreatedAt < tomorrowStart),
        
                    TodayOrderTotal = b.Orders
                        .Where(o => o.CreatedAt >= todayStart && o.CreatedAt < tomorrowStart)
                        .Sum(o => (decimal?)o.TotalAmount) ?? 0m
                })
                .FirstOrDefaultAsync();

            if (branchData == null)
            {
                return Result<BranchGetByIdResponse>.Failure(new NotFoundError(errCode, "Branch does not exist"));
            }

            var branchDto = new BranchGetByIdResponse
            {
                Id = branchData.Id,
                Name = branchData.Name,
                Address = branchData.Address,
                Merchant = branchData.Merchant,
                TodayOrderCount = branchData.TodayOrderCount,
                TodayOrderPrice = branchData.TodayOrderTotal, 
            };

            return Result<BranchGetByIdResponse>.Success(branchDto);
        }
        catch (Exception e)
        {
            return Result<BranchGetByIdResponse>.Failure(new InternalError(errCode, e.Message));
        }
    }

    public async Task<Result> Create(BranchCreateRequest request)
    {
        const string errCode = "Branch.Create";
        try
        {
            var isMerchantExist = await _db.Merchants.AnyAsync(m => m.Id == request.MerchantId);
            if (!isMerchantExist)
                return Result.Failure(new NotFoundError(errCode, "Merchant does not exist"));

            var isExist =
                await _db.Branches.AnyAsync(b => b.Name == request.Name && b.MerchantId == request.MerchantId);
            if (isExist)
                return Result.Failure(new ConflictError(errCode, "Branch already exist for this merchant"));

            var branch = new Branch
            {
                MerchantId = request.MerchantId,
                Name = request.Name,
                Address = request.Address,
                CreatedAt = DateTime.UtcNow
            };

            await _db.Branches.AddAsync(branch);
            var result = await _db.SaveChangesAsync();
            return result > 0
                ? Result.Success()
                : Result.Failure(new InternalError("Branch.Create", "Failed to create branch"));
        }
        catch (Exception e)
        {
            return Result.Failure(new InternalError(errCode, e.Message));
        }
    }

    public async Task<Result> Update(Guid branchId, BranchUpdateRequest request)
    {
        const string errCode = "Branch.Update";
        try
        {
            var branch = await _db.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == branchId);
            if (branch == null)
                return Result.Failure(new NotFoundError(errCode, "Branch does not exist"));

            if (branch.MerchantId != request.MerchantId)
            {
                var isMerchantExist =
                    await _db.Merchants.AnyAsync(m => m.Id == request.MerchantId);
                if (!isMerchantExist)
                    return Result.Failure(new NotFoundError(errCode, "Merchant does not exist"));
            }

            branch.MerchantId = request.MerchantId;
            branch.Name = request.Name;
            branch.Address = request.Address;
            branch.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Result.Success();
        }
        catch (Exception e)
        {
            return Result.Failure(new InternalError("Branch.Update", e.Message));
        }
    }

    public async Task<Result> Delete(Guid id)
    {
        const string errCode = "Branch.Delete";
        try
        {
            var branch = await _db.Branches.FirstOrDefaultAsync(b => b.Id == id && b.DeletedAt == null);
            if (branch == null)
                return Result.Failure(new NotFoundError(errCode, "Branch does not exist"));

            branch.DeletedAt = DateTime.UtcNow;
            var result = await _db.SaveChangesAsync();

            return result > 0
                ? Result.Success()
                : Result.Failure(new InternalError(errCode, "Failed to delete branch"));
        }
        catch (Exception e)
        {
            return Result.Failure(new InternalError(errCode, e.Message));
        }
    }
}

public class BranchListRequest : PaginationFilter
{
    public Guid MerchantId { get; set; }
    public bool IncludeMerchant { get; set; } = false;
}

public class MerchantDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string? ContactEmail { get; set; }
}

public class BranchListResponse
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Address { get; set; }
    public MerchantDto? Merchant { get; set; }
}

public class BranchGetByIdResponse
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Address { get; set; }
    public decimal TodayOrderPrice { get; set; }
    public int TodayOrderCount { get; set; }
    public MerchantDto? Merchant { get; set; }
}

public class BranchCreateRequest
{
    public Guid MerchantId { get; set; }
    public string Name { get; set; } = null!;
    public string? Address { get; set; }
}

public class BranchUpdateRequest
{
    public Guid MerchantId { get; set; }
    public string Name { get; set; } = null!;
    public string? Address { get; set; }
}