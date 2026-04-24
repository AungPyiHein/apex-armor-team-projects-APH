using Microsoft.EntityFrameworkCore;
using POS.Backend.Common;
using POS.data.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POS.Backend.Features.Category
{


    public class CategoryResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public int ProductCount { get; set; }

    }
    public class CreateCategoryRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid MerchantId { get; set; }
    }
    public class UpdateCategoryRequest
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
    }

    public interface ICategoryServices
    {
        Task<Result<PagedResponse<CategoryResponseDto>>> GetAllCategoriesAsync(PaginationFilter filter);
        Task<Result<CategoryResponseDto>> GetCategoryByIdAsync(Guid id);
        Task<Result<Guid>> CreateCategoryAsync(CreateCategoryRequest request);
        Task<Result> UpdateCategoryAsync(UpdateCategoryRequest request);
        Task<Result> DeleteCategoryAsync(Guid id);
        Task<Result> RestoreCategoryAsync(Guid id);
        Task<Result<PagedResponse<CategoryResponseDto>>> GetDeletedCategoriesAsync(PaginationFilter filter);
    }

    public class CategoryService : ICategoryServices
    {
        private readonly AppDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public CategoryService(AppDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }
        public async Task<Result<PagedResponse<CategoryResponseDto>>> GetAllCategoriesAsync(PaginationFilter filter)
        {
            var query = _context.Categories.AsNoTracking().Where(c => c.DeletedAt == null).AsQueryable();

            if (_currentUser.Role == POS.Shared.Models.UserRole.MerchantAdmin || _currentUser.Role == POS.Shared.Models.UserRole.Staff)
            {
                query = query.Where(c => c.MerchantId == _currentUser.MerchantId);
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                query = query.Where(c => EF.Functions.Like(c.Name, $"%{filter.SearchTerm}%"));
            }

            var totalRecords = await query.CountAsync();

            var category = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(c => new CategoryResponseDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    ProductCount = _context.Products.Count(p => p.CategoryId == c.Id && p.DeletedAt == null)
                }).ToListAsync();

            var pagedResponse = new PagedResponse<CategoryResponseDto>(category, totalRecords, filter.PageNumber, filter.PageSize);
            return Result<PagedResponse<CategoryResponseDto>>.Success(pagedResponse);
        }
        public async Task<Result<CategoryResponseDto>> GetCategoryByIdAsync(Guid id)
        {
            var query = _context.Categories
                .AsNoTracking()
                .Include(c => c.Merchant)
                .Where(c => c.Id == id && c.DeletedAt == null);

            if (_currentUser.Role == POS.Shared.Models.UserRole.MerchantAdmin || _currentUser.Role == POS.Shared.Models.UserRole.Staff)
            {
                query = query.Where(c => c.MerchantId == _currentUser.MerchantId);
            }

            var category = await query
                .Select(c => new CategoryResponseDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    ProductCount = _context.Products.Count(p => p.CategoryId == c.Id && p.DeletedAt == null)
                }).FirstOrDefaultAsync();
            if (category == null)
                return Result<CategoryResponseDto>.Failure("Category not found");
            return Result<CategoryResponseDto>.Success(category);
        }

        public async Task<Result<Guid>> CreateCategoryAsync(CreateCategoryRequest request)
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

            var merchantExists = await _context.Merchants.AnyAsync(m => m.Id == request.MerchantId && m.DeletedAt == null);
            if (!merchantExists)
            {
                return Result<Guid>.Failure("Merchant not found. Please provide a valid Merchant ID.");
            }
            var category = new data.Entities.Category
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                MerchantId = request.MerchantId
            };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return Result<Guid>.Success(category.Id);
        }
        public async Task<Result> UpdateCategoryAsync(UpdateCategoryRequest request)
        {
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == request.Id && c.DeletedAt == null);
            if (category == null)
                return Result.Failure("Category not found");
            if (!string.IsNullOrEmpty(request.Name))
                category.Name = request.Name;
            if (!string.IsNullOrEmpty(request.Description))
                category.Description = request.Description;
            category.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Result.Success();
        }
        public async Task<Result> DeleteCategoryAsync(Guid id)
        {
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null);

            if (category == null) return Result.Failure("Category not found or already Deleted.");
            var hasProducts = await _context.Products.AnyAsync(p => p.CategoryId == id && p.DeletedAt == null);

            if (hasProducts)
            {
                return Result.Failure("Cannot delete a category that still contains products.");
            }

            category.DeletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Result.Success();
        }
        public async Task<Result> RestoreCategoryAsync(Guid id)
        {
            var category = await _context.Categories
                .IgnoreQueryFilters()
                .Include(c => c.Merchant)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null) return Result.Failure("Category not found.");
            if (category.DeletedAt == null) return Result.Failure("Category is not deleted.");

            if (category.Merchant != null && category.Merchant.DeletedAt != null)
            {
                return Result.Failure("Cannot restore this category because the Merchant is deleted.");
            }

            category.DeletedAt = null;
            await _context.SaveChangesAsync();
            return Result.Success();
        }

        public async Task<Result<PagedResponse<CategoryResponseDto>>> GetDeletedCategoriesAsync(PaginationFilter filter)
        {
            var query = _context.Categories
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(c => c.DeletedAt != null)
                .AsQueryable();

            if (_currentUser.Role == POS.Shared.Models.UserRole.MerchantAdmin || _currentUser.Role == POS.Shared.Models.UserRole.Staff)
            {
                query = query.Where(c => c.MerchantId == _currentUser.MerchantId);
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                query = query.Where(c => EF.Functions.Like(c.Name, $"%{filter.SearchTerm}%"));
            }

            var totalRecords = await query.CountAsync();

            var categories = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(c => new CategoryResponseDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    ProductCount = _context.Products.IgnoreQueryFilters().Count(p => p.CategoryId == c.Id && p.DeletedAt == null)
                }).ToListAsync();

            var pagedResponse = new PagedResponse<CategoryResponseDto>(categories, totalRecords, filter.PageNumber, filter.PageSize);
            return Result<PagedResponse<CategoryResponseDto>>.Success(pagedResponse);
        }
    }
}
