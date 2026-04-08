using Common;
using Database.EfAppDbContextModels;
using Microsoft.EntityFrameworkCore;
using MiniPos.Backend.Features.Users;

namespace MiniPos.Backend.Features.Products;

public interface IProductService
{
    Task<Result<PagedResult<ProductListResponse>>> GetList(ProductListRequest request);
    Task<Result<ProductGetByIdResponse>> GetById(Guid id);
    Task<Result<ProductCreateResponse>> Create(ProductCreateRequest request);
    Task<Result> Update(Guid id, ProductUpdateRequest request);
    Task<Result> Delete(Guid id);
}

public class ProductService : IProductService
{
    private readonly AppDbContext _db;

    public ProductService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PagedResult<ProductListResponse>>> GetList(ProductListRequest request)
    {
        try
        {
            var query = _db.Products
                .Where(x => x.MerchantId == request.MerchantId)
                .AsNoTracking()
                .AsQueryable();

            if (request.CategoryId.HasValue)
                query = query.Where(x => x.CategoryId == request.CategoryId.Value);

            if (!string.IsNullOrEmpty(request.SearchTerm))
                query = query.Where(x => x.Name.Contains(request.SearchTerm) || x.Sku.Contains(request.SearchTerm));

            var skip = (request.PageNumber - 1) * request.PageSize;
            var take = request.PageSize;
            var totalCount = await query.CountAsync();
            
            var products = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip(skip)
                .Take(take)
                .Select(p => new ProductListResponse
                {
                    Id = p.Id,
                    Name = p.Name,
                    Sku = p.Sku,
                    Price = p.Price,
                    Merchant = new MerchantDto
                    {
                        Id = p.MerchantId,
                        Name = p.Merchant.Name,
                    },
                    Category = new CategoryDto
                    {
                        Id = p.CategoryId,
                        Name = p.Category.Name,
                    }
                })
                .ToListAsync();

            var result = new PagedResult<ProductListResponse>(products, totalCount, request.PageNumber, request.PageSize);
            return Result<PagedResult<ProductListResponse>>.Success(result);
        }
        catch (Exception e)
        {
            return Result<PagedResult<ProductListResponse>>.Failure(new InternalError("Product.GetList", e.Message));
        }
    }

    public async Task<Result<ProductGetByIdResponse>> GetById(Guid id)
    {
        try
        {
            var product = await _db.Products
                .AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => new ProductGetByIdResponse
                {
                    Id = p.Id,
                    Name = p.Name,
                    Sku = p.Sku,
                    Price = p.Price,
                    CreatedAt = p.CreatedAt,
                })
                .FirstOrDefaultAsync();

            if (product is null)
                return Result<ProductGetByIdResponse>.Failure(new NotFoundError("Product.GetById", "Product not found"));

            return Result<ProductGetByIdResponse>.Success(product);
        }
        catch (Exception e)
        {
            return Result<ProductGetByIdResponse>.Failure(new InternalError("Product.GetById", e.Message));
        }
    }

    public async Task<Result<ProductCreateResponse>> Create(ProductCreateRequest request)
    {
        try
        {
            var merchantExists = await _db.Merchants.AnyAsync(m => m.Id == request.MerchantId && m.IsActive);
            if (!merchantExists)
                return Result<ProductCreateResponse>.Failure(new NotFoundError("Product.Create", "Merchant not found or inactive"));

            var categoryExists = await _db.Categories.AnyAsync(c => c.Id == request.CategoryId);
            if (!categoryExists)
                return Result<ProductCreateResponse>.Failure(new NotFoundError("Product.Create", "Category not found"));

            var isSkuExists = await _db.Products.AnyAsync(p => p.Sku == request.Sku && p.MerchantId == request.MerchantId);
            if (isSkuExists)
                return Result<ProductCreateResponse>.Failure(new ConflictError("Product.Create", "Product with this SKU already exists for this merchant"));

            var product = new Product
            {
                Id = Guid.NewGuid(),
                MerchantId = request.MerchantId,
                CategoryId = request.CategoryId,
                Name = request.Name,
                Sku = request.Sku,
                Price = request.Price,
                CreatedAt = DateTime.UtcNow
            };

            await _db.Products.AddAsync(product);
            var result = await _db.SaveChangesAsync();

            return result > 0 
                ? Result<ProductCreateResponse>.Success(new ProductCreateResponse { Id = product.Id })
                : Result<ProductCreateResponse>.Failure(new InternalError("Product.Create", "Failed to create product"));
        }
        catch (Exception e)
        {
            return Result<ProductCreateResponse>.Failure(new InternalError("Product.Create", e.Message));
        }
    }

    public async Task<Result> Update(Guid id, ProductUpdateRequest request)
    {
        try
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product is null)
                return Result.Failure(new NotFoundError("Product.Update", "Product not found"));

            if (request.CategoryId.HasValue)
            {
                var categoryExists = await _db.Categories.AnyAsync(c => c.Id == request.CategoryId.Value);
                if (!categoryExists)
                    return Result.Failure(new NotFoundError("Product.Update", "Category not found"));
                product.CategoryId = request.CategoryId.Value;
            }

            if (!string.IsNullOrEmpty(request.Sku) && request.Sku != product.Sku)
            {
                var isSkuExists = await _db.Products.AnyAsync(p => p.Sku == request.Sku && p.MerchantId == product.MerchantId && p.Id != id);
                if (isSkuExists)
                    return Result.Failure(new ConflictError("Product.Update", "Product with this SKU already exists for this merchant"));
                product.Sku = request.Sku;
            }

            if (!string.IsNullOrEmpty(request.Name))
                product.Name = request.Name;

            if (request.Price.HasValue)
                product.Price = request.Price.Value;

            product.UpdatedAt = DateTime.UtcNow;

            var result = await _db.SaveChangesAsync();
            return result > 0 
                ? Result.Success()
                : Result.Failure(new InternalError("Product.Update", "Failed to update product"));
        }
        catch (Exception e)
        {
            return Result.Failure(new InternalError("Product.Update", e.Message));
        }
    }

    public async Task<Result> Delete(Guid id)
    {
        try
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product is null)
                return Result.Failure(new NotFoundError("Product.Delete", "Product not found"));

            product.DeletedAt = DateTime.UtcNow;
            var result = await _db.SaveChangesAsync();

            return result > 0
                ? Result.Success()
                : Result.Failure(new InternalError("Product.Delete", "Failed to delete product"));
        }
        catch (Exception e)
        {
            return Result.Failure(new InternalError("Product.Delete", e.Message));
        }
    }
}

public class ProductListRequest : PaginationFilter
{
    public Guid MerchantId { get; set; }
    public Guid? CategoryId { get; set; }
    public string? SearchTerm { get; set; }
    public Guid ProcessedById { get; set; }
}

public class MerchantDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
}

public class CategoryDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
}

public class ProductListResponse
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Sku { get; set; }
    public decimal Price { get; set; }
    public MerchantDto? Merchant { get; set; }
    public CategoryDto? Category { get; set; }
}

public class ProductGetByIdResponse
{
    public Guid Id { get; set; }
    public Guid MerchantId { get; set; }
    public Guid CategoryId { get; set; }
    public string? Name { get; set; }
    public string? Sku { get; set; }
    public decimal Price { get; set; }
    public DateTime? CreatedAt { get; set; }
    public MerchantDto? Merchant { get; set; }
}

public class ProductCreateRequest
{
    public Guid MerchantId { get; set; }
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = null!;
    public string Sku { get; set; } = null!;
    public decimal Price { get; set; }
}

public class ProductCreateResponse
{
    public Guid Id { get; set; }
}

public class ProductUpdateRequest
{
    public Guid? CategoryId { get; set; }
    public string? Name { get; set; }
    public string? Sku { get; set; }
    public decimal? Price { get; set; }
}