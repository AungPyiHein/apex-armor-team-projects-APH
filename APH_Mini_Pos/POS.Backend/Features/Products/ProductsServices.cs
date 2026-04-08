using Microsoft.EntityFrameworkCore;
using POS.data.Data;
using POS.Backend.Common;

namespace POS.Backend.Features.Products
{
    public class ProductsResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string SKU { get; set; }
        public string? Description { get; set; }
        public string? Barcode { get; set; }
        public string CategoryName { get; set; }
        public string CategoryDescription { get; set; }
        public string MerchantName { get; set; }
        public Guid CategoryId { get; set; }
        public Guid MerchantId { get; set; }

    }
    public class CreateProductRequest
    {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string SKU { get; set; }
        public string? Description { get; set; }
        public string? Barcode { get; set; }
        public Guid CategoryId { get; set; }
        public Guid MerchantId { get; set; }
    }

    public class UpdateProductRequest
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public decimal? Price { get; set; }
        public string? SKU { get; set; }
        public string? Description { get; set; }
        public string? Barcode { get; set; }
        public Guid? CategoryId { get; set; }
    }
    public interface IProductsServices
    {
        Task<Result<PagedResponse<ProductsResponseDto>>> GetAllProductsAsync(PaginationFilter filter);
        Task<Result<ProductsResponseDto>> GetProductById(Guid id);
        Task<Result<Guid>> CreateProductAsync(CreateProductRequest request);
        Task<Result> UpdateProductAsync(UpdateProductRequest request);
        Task<Result> DeleteProductAsync(Guid id);
    }

    public class ProductsServices : IProductsServices
    {
        public readonly AppDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public ProductsServices(AppDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }
        public async Task<Result<PagedResponse<ProductsResponseDto>>> GetAllProductsAsync(PaginationFilter filter)
        {
            var query = _context.Products.AsNoTracking().Where(p => p.DeletedAt == null).AsQueryable();

            if (_currentUser.Role == POS.Shared.Models.UserRole.MerchantAdmin || _currentUser.Role == POS.Shared.Models.UserRole.Staff)
            {
                query = query.Where(p => p.MerchantId == _currentUser.MerchantId);
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                query = query.Where(p => EF.Functions.Like(p.Name, $"%{filter.SearchTerm}%") ||
                                         EF.Functions.Like(p.Sku, $"%{filter.SearchTerm}%"));
            }

            if (filter.CategoryId != null && filter.CategoryId != Guid.Empty)
            {
                query = query.Where(p => p.CategoryId == filter.CategoryId);
            }

            var totalRecords = await query.CountAsync();

            var products = await query
                .Include(p => p.Category)
                .Include(p => p.Merchant)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var response = products.Select(p => new ProductsResponseDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                SKU = p.Sku,
                Description = p.Description,
                Barcode = p.Barcode,
                CategoryName = p.Category?.Name ?? "No Category",
                CategoryDescription = p.Category?.Description ?? "No Description",
                CategoryId = p.CategoryId,
                MerchantName = p.Merchant?.Name ?? "Unknown Merchant",
                MerchantId = p.MerchantId
            }).ToList();

            var pagedResponse = new PagedResponse<ProductsResponseDto>(response, totalRecords, filter.PageNumber, filter.PageSize);
            return Result<PagedResponse<ProductsResponseDto>>.Success(pagedResponse);
        }
        public async Task<Result<ProductsResponseDto>> GetProductById(Guid id)
        {
            var query = _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Merchant)
                .Where(p => p.Id == id && p.DeletedAt == null);

            if (_currentUser.Role == POS.Shared.Models.UserRole.MerchantAdmin || _currentUser.Role == POS.Shared.Models.UserRole.Staff)
            {
                query = query.Where(p => p.MerchantId == _currentUser.MerchantId);
            }

            var product = await query
                .Select(p => new ProductsResponseDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    SKU = p.Sku,
                    Description = p.Description,
                    Barcode = p.Barcode,
                    CategoryName = p.Category != null ? p.Category.Name : "No Category",
                    CategoryDescription = p.Category != null ? p.Category.Description : "No Description",
                    CategoryId = p.CategoryId,
                    MerchantName = p.Merchant != null ? p.Merchant.Name : "Unknown Merchant",
                    MerchantId = p.MerchantId
                })
                .FirstOrDefaultAsync();
            if (product == null)
                return Result<ProductsResponseDto>.Failure("Product not found");
            return Result<ProductsResponseDto>.Success(product);

        }

        public async Task<Result<Guid>> CreateProductAsync(CreateProductRequest request)
        {
            if (_currentUser.Role == POS.Shared.Models.UserRole.MerchantAdmin || _currentUser.Role == POS.Shared.Models.UserRole.Staff)
            {
                if (_currentUser.MerchantId.HasValue)
                {
                    request.MerchantId = _currentUser.MerchantId.Value;
                }
                else
                {
                    return Result<Guid>.Failure("Merchant ID is missing from user claims.");
                }
            }

            var categoryExist = await _context.Categories.AnyAsync(c => c.Id == request.CategoryId && c.DeletedAt == null);
            if (!categoryExist)
                return Result<Guid>.Failure("Category not found or is deleted.");

            var merchantExist = await _context.Merchants.AnyAsync(m => m.Id == request.MerchantId && m.DeletedAt == null);
            if (!merchantExist)
                return Result<Guid>.Failure("Merchant not found or is deleted.");

            var newProduct = new data.Entities.Product
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Price = request.Price,
                Sku = request.SKU,
                Description = request.Description,
                Barcode = request.Barcode,
                CategoryId = request.CategoryId,
                MerchantId = request.MerchantId,
                CreatedAt = DateTime.UtcNow
            };
            _context.Products.Add(newProduct);
            await _context.SaveChangesAsync();

            return Result<Guid>.Success(newProduct.Id);
        }

        public async Task<Result> UpdateProductAsync(UpdateProductRequest request)
        {
            var existingProduct = await _context.Products.FirstOrDefaultAsync(p => p.Id == request.Id && p.DeletedAt == null);
            if (existingProduct == null)
                return Result.Failure("Product not found");
            if (!string.IsNullOrWhiteSpace(request.Name))
                existingProduct.Name = request.Name;

            if (request.Price.HasValue)
                existingProduct.Price = request.Price.Value;

            if (!string.IsNullOrWhiteSpace(request.SKU))
                existingProduct.Sku = request.SKU;

            if (!string.IsNullOrWhiteSpace(request.Description))
                existingProduct.Description = request.Description;

            if (!string.IsNullOrWhiteSpace(request.Barcode))
                existingProduct.Barcode = request.Barcode;

            if (request.CategoryId.HasValue && request.CategoryId != Guid.Empty)
            {
                var categoryExists = await _context.Categories.AnyAsync(c => c.Id == request.CategoryId && c.DeletedAt == null);
                if (!categoryExists) return Result.Failure("The selected Category does not exist.");

                existingProduct.CategoryId = request.CategoryId.Value;
            }

            existingProduct.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Result.Success();
        }
        public async Task<Result> DeleteProductAsync(Guid id)
        {
            var existingProduct = await _context.Products.FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null);
            if (existingProduct == null)
                return Result.Failure("Product not found");

            existingProduct.DeletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Result.Success();
        }
    }
}