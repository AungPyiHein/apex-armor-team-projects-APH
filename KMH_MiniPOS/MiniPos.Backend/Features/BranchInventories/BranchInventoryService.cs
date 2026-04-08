using Common;
using Database.EfAppDbContextModels;
using Microsoft.EntityFrameworkCore;

namespace MiniPos.Backend.Features.BranchInventories;

public interface IBranchInventoryService
{
    Task<Result<PagedResult<BranchInventoryListResponse>>> GetList(BranchInventoryListRequest request);
    Task<Result<BranchInventoryGetByIdResponse>> GetById(Guid id);
    Task<Result> Create(BranchInventoryCreateRequest request);
    Task<Result> Update(Guid id, BranchInventoryUpdateRequest request);
    Task<Result> Delete(Guid id);
}

public class BranchInventoryService : IBranchInventoryService
{
    private readonly AppDbContext _db;

    public BranchInventoryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PagedResult<BranchInventoryListResponse>>> GetList(
        BranchInventoryListRequest request)
    {
        try
        {
            var isBranchExists = await _db.Branches.AnyAsync(branch => branch.Id == request.BranchId);
            if (!isBranchExists)
                return Result<PagedResult<BranchInventoryListResponse>>.Failure(new NotFoundError("Product.GetList",
                    "Branch does not exist"));

            var query = _db.BranchInventories
                .Where(inventory => inventory.BranchId == request.BranchId)
                .AsNoTracking()
                .AsQueryable();

            if (request.CategoryId.HasValue)
                query = query.Where(inventory => inventory.Product.CategoryId == request.CategoryId);

            var totalCount = await query.CountAsync();
            var skip = (request.PageNumber - 1) * request.PageSize;
            var take = request.PageSize;
            var items = await query
                .Skip(skip)
                .Take(take)
                .OrderByDescending(product => product.CreatedAt)
                .Select(inventory => new BranchInventoryListResponse
                {
                    Id = inventory.Id,
                    ProductId = inventory.ProductId,
                    Name = inventory.Product.Name,
                    Sku = inventory.Product.Sku,
                    Price = inventory.Product.Price,
                    StockQuantity = inventory.StockQuantity
                })
                .ToListAsync();
            
            var result =
                new PagedResult<BranchInventoryListResponse>(items, totalCount, request.PageNumber,
                    request.PageSize);
            return Result<PagedResult<BranchInventoryListResponse>>.Success(result);
        }
        catch (Exception e)
        {
            return Result<PagedResult<BranchInventoryListResponse>>.Failure(new InternalError("Product.GetList",
                e.Message));
        }
    }

    public async Task<Result<BranchInventoryGetByIdResponse>> GetById(Guid id)
    {
        const string errCode = "Product.GetById";
        try
        {
            var products =
                await _db.BranchInventories
                    .AsNoTracking()
                    .Include(inventory => inventory.Product)
                    .Where(inventory => inventory.Id == id)
                    .Select(inventory => new BranchInventoryGetByIdResponse
                    {
                        Id = inventory.Id,
                        ProductId = inventory.ProductId, // <-- Added
                        BranchId = inventory.BranchId,
                        BranchName = inventory.Branch.Name,
                        StockQuantity = inventory.StockQuantity,
                        Name = inventory.Product.Name,
                        Price = inventory.Product.Price,
                        Sku = inventory.Product.Sku,
                        CategoryName = inventory.Product.Category.Name
                    })
                    .ToListAsync();

            return products.Count == 0
                ? Result<BranchInventoryGetByIdResponse>.Failure(
                    new NotFoundError(errCode, "Product does not exist"))
                : Result<BranchInventoryGetByIdResponse>.Success(products[0]);
        }
        catch (Exception e)
        {
            return Result<BranchInventoryGetByIdResponse>.Failure(new InternalError(errCode, e.Message));
        }
    }

    public async Task<Result> Create(BranchInventoryCreateRequest request)
    {
        const string errCode = "Product.Create";
        try
        {
            var merchantExists = await _db.Merchants.AnyAsync(m => m.Id == request.MerchantId);
            if (!merchantExists)
                return Result.Failure(new BadRequestError(errCode, "Merchant does not exist"));

            var productExist =
                await _db.Products.AnyAsync(p => p.Id == request.ProductId && p.MerchantId == request.MerchantId);
            if (!productExist)
                return Result.Failure(new BadRequestError(errCode, "Product does not exist for this merchant"));

            var branchExist = await _db.Branches
                .AnyAsync(b => b.Id == request.BranchId && b.MerchantId == request.MerchantId);
            if (!branchExist)
                return Result.Failure(new BadRequestError(errCode, "Branch does not exist for this merchant"));

            var branchInventory = new BranchInventory
            {
                BranchId = request.BranchId,
                ProductId = request.ProductId,
                StockQuantity = request.StockQuantity
            };
            
            await _db.BranchInventories.AddAsync(branchInventory);
            await _db.SaveChangesAsync();
            return Result.Success();
        }
        catch (Exception e)
        {
            Console.WriteLine($"{errCode} ${e.Message}");
            return Result.Failure(new InternalError(errCode, e.Message));
        }
    }

    public async Task<Result> Update(Guid id, BranchInventoryUpdateRequest request)
    {
        const string errCode = "Product.Update";
        try
        {
            var inventory = await _db.BranchInventories
                .Include(inventory => inventory.Product)
                .FirstOrDefaultAsync(inventory => inventory.Id == id);
            if (inventory == null)
                return Result.Failure(new NotFoundError(errCode, "Product does not exist"));

            if (request.Price.HasValue)
            {
                inventory.Product.Price = request.Price.Value;
                inventory.Product.UpdatedAt = DateTime.UtcNow;
            }

            if (request.StockQuantity.HasValue)
            {
                inventory.StockQuantity = request.StockQuantity.Value;
                inventory.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
            return Result.Success();
        }
        catch (Exception e)
        {
            return Result.Failure(new InternalError(errCode, e.Message));
        }
    }

    public async Task<Result> Delete(Guid id)
    {
        const string errCode = "Product.Delete";
        try
        {
            var product = await _db.BranchInventories.FirstOrDefaultAsync(inventory => inventory.Id == id);
            if (product == null)
                return Result.Failure(new NotFoundError(errCode, "Product does not exist"));

            product.DeletedAt = DateTime.UtcNow;
            var result = await _db.SaveChangesAsync();

            return result > 0
                ? Result.Success()
                : Result.Failure(new InternalError(errCode, "Failed to delete product"));
        }
        catch (Exception e)
        {
            return Result.Failure(new InternalError(errCode, e.Message));
        }
    }
}

public class BranchInventoryListRequest : PaginationFilter
{
    public Guid ProcessedById { get; set; }
    public Guid BranchId { get; set; }
    public Guid? CategoryId { get; set; }
}

public class BranchInventoryListResponse
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string Name { get; set; }
    public string Sku { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string? CategoryName { get; set; }
}

public class BranchInventoryGetByIdResponse
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Sku { get; set; } = null!;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string? CategoryName { get; set; }
}

public class BranchInventoryCreateRequest
{
    public Guid MerchantId { get; set; }
    public Guid BranchId { get; set; }
    public Guid ProductId { get; set; }
    public int StockQuantity { get; set; }
}

public class BranchInventoryUpdateRequest
{
    public decimal? Price { get; set; }
    public int? StockQuantity { get; set; }
}