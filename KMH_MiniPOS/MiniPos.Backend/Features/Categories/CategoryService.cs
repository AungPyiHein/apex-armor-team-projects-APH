using Common;
using Database.EfAppDbContextModels;
using Microsoft.EntityFrameworkCore;

namespace MiniPos.Backend.Features.Categories;

public interface ICategoryService
{
    Task<Result<PagedResult<CategoryListResponse>>> GetList(CategoryListRequest request);
    Task<Result<CategoryGetByIdResponse>> GetById(Guid categoryId);
    Task<Result> Create(CategoryCreateRequest request);
    Task<Result> Update(Guid id, CategoryUpdateRequest request);
    Task<Result> Delete(Guid id);
}

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _db;

    public CategoryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PagedResult<CategoryListResponse>>> GetList(CategoryListRequest request)
    {
        try
        {
            if (!await IsMerchantOwner(request.MerchantId, request.ProcessedById))
                return Result<PagedResult<CategoryListResponse>>.Failure(new UnAuthorizedError("Category.GetList",
                    "User is not authorized to access categories for this merchant"));

            var query = _db.Categories
                .Where(c => c.MerchantId == request.MerchantId)
                .AsNoTracking()
                .AsQueryable();

            var skip = (request.PageNumber - 1) * request.PageSize;
            var take = request.PageSize;
            var totalCount = await query.CountAsync();

            var items = await query
                .AsNoTracking()
                .Skip(skip)
                .Take(take)
                .OrderByDescending(category => category.CreatedAt)
                .Select(c => new CategoryListResponse
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    MerchantId = c.Merchant.Id,
                    MerchantName = c.Merchant.Name,
                    ProductCount = c.Products.Count()
                })
                .ToListAsync();

            var result =
                new PagedResult<CategoryListResponse>(items, totalCount, request.PageNumber, request.PageSize);
            return Result<PagedResult<CategoryListResponse>>.Success(result);
        }
        catch (Exception e)
        {
            return Result<PagedResult<CategoryListResponse>>.Failure(
                new InternalError("Category.GetList", e.Message));
        }
    }

    public async Task<Result<CategoryGetByIdResponse>> GetById(Guid categoryId)
    {
        const string errCode = "Category.GetById";
        try
        {
            var categoryDto = await _db.Categories
                .AsNoTracking()
                .Where(c => c.Id == categoryId)
                .Select(c => new CategoryGetByIdResponse
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    CreatedAt = c.CreatedAt,
                    Merchant = new MerchantDto
                    {
                        Id = c.MerchantId,
                        Name = c.Merchant.Name
                    },
                    Products = c.Products.Select(p => new ProductDto
                    {
                        Id = p.Id,
                        Name = p.Name
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (categoryDto == null)
                return Result<CategoryGetByIdResponse>.Failure(
                    new NotFoundError(errCode, "Category does not exist"));

            return Result<CategoryGetByIdResponse>.Success(categoryDto);
        }
        catch (Exception e)
        {
            return Result<CategoryGetByIdResponse>.Failure(new InternalError(errCode, e.Message));
        }
    }

    public async Task<Result> Create(CategoryCreateRequest request)
    {
        const string errCode = "Category.Create";
        try
        {
            var merchantExists = await _db.Merchants.AnyAsync(m => m.Id == request.MerchantId);
            if (!merchantExists)
                return Result.Failure(new NotFoundError(errCode, "Merchant does not exist"));

            var isCategoryAlreadyExist =
                await _db.Categories.AnyAsync(c => c.Name == request.Name && c.MerchantId == request.MerchantId);
            if (isCategoryAlreadyExist)
                return Result.Failure(new ConflictError(errCode,
                    "Category with the same name already exists for this merchant"));

            var category = new Category
            {
                MerchantId = request.MerchantId,
                Name = request.Name,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow
            };

            await _db.Categories.AddAsync(category);
            var result = await _db.SaveChangesAsync();
            return result > 0
                ? Result.Success()
                : Result.Failure(new InternalError(errCode, "Failed to create category"));
        }
        catch (Exception e)
        {
            return Result.Failure(new InternalError(errCode, e.Message));
        }
    }

    public async Task<Result> Update(Guid id, CategoryUpdateRequest request)
    {
        const string errCode = "Category.Update";
        try
        {
            var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id);
            if (category == null)
                return Result.Failure(new NotFoundError(errCode, "Category does not exist"));

            if (category.MerchantId != request.MerchantId)
            {
                var merchantExists = await _db.Merchants.AnyAsync(m => m.Id == request.MerchantId);
                if (!merchantExists)
                    return Result.Failure(new NotFoundError(errCode, "Merchant does not exist"));
            }

            category.MerchantId = request.MerchantId;
            category.Name = request.Name;
            category.Description = request.Description;
            category.UpdatedAt = DateTime.UtcNow;

            var result = await _db.SaveChangesAsync();
            return result > 0
                ? Result.Success()
                : Result.Failure(new InternalError(errCode, "Failed to update category"));
        }
        catch (Exception e)
        {
            return Result.Failure(new InternalError(errCode, e.Message));
        }
    }

    public async Task<Result> Delete(Guid id)
    {
        const string errCode = "Category.Delete";
        try
        {
            var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id);
            if (category == null)
                return Result.Failure(new NotFoundError(errCode, "Category does not exist"));

            category.DeletedAt = DateTime.UtcNow;
            var result = await _db.SaveChangesAsync();

            return result > 0
                ? Result.Success()
                : Result.Failure(new InternalError(errCode, "Failed to delete category"));
        }
        catch (Exception e)
        {
            return Result.Failure(new InternalError(errCode, e.Message));
        }
    }

    public async Task<bool> IsMerchantOwner(Guid merchantId, Guid merchantAdminId)
    {
        return await _db.MerchantAdmins.AnyAsync(ma => ma.MerchantId == merchantId && ma.UserId == merchantAdminId);
    }
}

public class CategoryListRequest : PaginationFilter
{
    public Guid ProcessedById { get; set; }
    public Guid MerchantId { get; set; }
}

public class MerchantDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
}

public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
}

public class CategoryListResponse
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public Guid MerchantId { get; set; }
    public string? MerchantName { get; set; }
    public int ProductCount { get; set; }
}

public class CategoryGetByIdResponse
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public MerchantDto? Merchant { get; set; }
    public List<ProductDto> Products { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class CategoryCreateRequest
{
    public Guid MerchantId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}

public class CategoryUpdateRequest
{
    public Guid MerchantId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}