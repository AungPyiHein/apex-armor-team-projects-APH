using Microsoft.EntityFrameworkCore;
using POS.Backend.Common;
using POS.data.Data;


namespace POS.Backend.Features.Merchants
{
    public class CreateMerchantRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? ContactEmail { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
    }
    public class MerchantResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string ContactEmail { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public bool isActive { get; set; }
        public int CategoryCount { get; set; }
        public int ProductCount { get; set; }
    }

    public class UpdateMerchantRequest
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? ContactEmail { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public bool? IsActive { get; set; }
    }

    public interface IMerchantsServices
    {
        Task<Result<PagedResponse<MerchantResponseDto>>> GetAllMerchantsAsync(PaginationFilter filter);
        Task<Result<MerchantResponseDto>> GetMerchantByIdAsync(Guid id);
        Task<Result<Guid>> CreateMerchantAsync(CreateMerchantRequest request);
        Task<Result> UpdateMerchantAsync(UpdateMerchantRequest request);
        Task<Result> DeleteMerchantAsync(Guid id, bool force = false);
        Task<Result<PagedResponse<MerchantResponseDto>>> GetDeletedMerchantsAsync(PaginationFilter filter);
        Task<Result> RestoreMerchantAsync(Guid id, bool restoreAll = false);
    }

    public class MerchantsServices : IMerchantsServices
    {
        private readonly AppDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public MerchantsServices(AppDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }
        public async Task<Result<PagedResponse<MerchantResponseDto>>> GetAllMerchantsAsync(PaginationFilter filter)
        {
            var query = _context.Merchants
                .AsNoTracking()
                .Where(m => m.DeletedAt == null)
                .AsQueryable();

            if (_currentUser.Role == POS.Shared.Models.UserRole.MerchantAdmin)
            {
                query = query.Where(m => m.Id == _currentUser.MerchantId);
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                query = query.Where(m => EF.Functions.Like(m.Name, $"%{filter.SearchTerm}%") ||
                                         EF.Functions.Like(m.ContactEmail, $"%{filter.SearchTerm}%"));
            }

            var totalRecords = await query.CountAsync();

            var merchants = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(m => new MerchantResponseDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    ContactEmail = m.ContactEmail,
                    Address = m.Address,
                    PhoneNumber = m.PhoneNumber,
                    isActive = m.IsActive,
                    CategoryCount = m.Categories.Count(c => c.DeletedAt == null),
                    ProductCount = m.Products.Count(p => p.DeletedAt == null)
                })
                .ToListAsync();

            var pagedResponse = new PagedResponse<MerchantResponseDto>(merchants, totalRecords, filter.PageNumber, filter.PageSize);
            return Result<PagedResponse<MerchantResponseDto>>.Success(pagedResponse);
        }

        public async Task<Result<PagedResponse<MerchantResponseDto>>> GetDeletedMerchantsAsync(PaginationFilter filter)
        {
            var query = _context.Merchants
                .IgnoreQueryFilters()
                .Where(m => m.DeletedAt != null)
                .AsQueryable();

            if (_currentUser.Role == POS.Shared.Models.UserRole.MerchantAdmin)
            {
                query = query.Where(m => m.Id == _currentUser.MerchantId);
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                query = query.Where(m => EF.Functions.Like(m.Name, $"%{filter.SearchTerm}%") ||
                                         EF.Functions.Like(m.ContactEmail, $"%{filter.SearchTerm}%"));
            }

            var totalRecords = await query.CountAsync();

            var merchants = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(m => new MerchantResponseDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    ContactEmail = m.ContactEmail,
                    Address = m.Address,
                    PhoneNumber = m.PhoneNumber,
                    isActive = m.IsActive,
                    CategoryCount = m.Categories.Count(),
                    ProductCount = m.Products.Count()
                })
                .ToListAsync();

            var pagedResponse = new PagedResponse<MerchantResponseDto>(merchants, totalRecords, filter.PageNumber, filter.PageSize);
            return Result<PagedResponse<MerchantResponseDto>>.Success(pagedResponse);
        }

        public async Task<Result<Guid>> CreateMerchantAsync(CreateMerchantRequest request)
        {
            if (!string.IsNullOrEmpty(request.ContactEmail))
            {
                var emailExists = await _context.Merchants
                    .AnyAsync(m => m.ContactEmail == request.ContactEmail && m.DeletedAt == null);
                if (emailExists)
                    return Result<Guid>.Failure("Email is already registered to another merchant.");
            }
            var merchant = new data.Entities.Merchant
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                ContactEmail = request.ContactEmail,
                Address = request.Address,
                PhoneNumber = request.PhoneNumber,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.Merchants.Add(merchant);
            await _context.SaveChangesAsync();

            return Result<Guid>.Success(merchant.Id);
        }
        public async Task<Result> DeleteMerchantAsync(Guid id, bool force = false)
        {
            var merchant = await _context.Merchants
                .Include(m => m.Branches.Where(b => b.DeletedAt == null))
                .Include(m => m.Users.Where(u => u.DeletedAt == null))
                .Include(m => m.Products.Where(p => p.DeletedAt == null))
                .Include(m => m.Categories.Where(c => c.DeletedAt == null))
                .FirstOrDefaultAsync(m => m.Id == id && m.DeletedAt == null);

            if (merchant == null)
                return Result.Failure("Merchant not found.");

            bool hasActiveDependencies = merchant.Branches.Any() || merchant.Users.Any() || merchant.Products.Any() || merchant.Categories.Any();

            if (hasActiveDependencies && !force)
            {
                // Return a specific failure string we can catch in frontend
                return Result.Failure($"DependenciesExist:Cannot delete merchant because it has active dependencies ({merchant.Branches.Count} branches, {merchant.Users.Count} users, {merchant.Products.Count} products, {merchant.Categories.Count} categories). Please use force delete.");
            }

            var now = DateTime.UtcNow;

            if (force && hasActiveDependencies)
            {
                foreach (var user in merchant.Users) { user.DeletedAt = now; user.IsActive = false; }
                foreach (var branch in merchant.Branches) { branch.DeletedAt = now; }
                foreach (var category in merchant.Categories) { category.DeletedAt = now; }
                foreach (var product in merchant.Products) { product.DeletedAt = now; }
            }

            merchant.DeletedAt = now;
            merchant.IsActive = false;
            await _context.SaveChangesAsync();

            return Result.Success();
        }
        public async Task<Result<MerchantResponseDto>> GetMerchantByIdAsync(Guid id)
        {
            var merchants = await _context.Merchants
                 .AsNoTracking()
                 .Where(m => m.DeletedAt == null && m.Id == id)
                 .Select(m => new MerchantResponseDto
                 {
                     Id = m.Id,
                     Name = m.Name,
                     ContactEmail = m.ContactEmail,
                     Address = m.Address,
                     PhoneNumber = m.PhoneNumber,
                     isActive = m.IsActive,
                     CategoryCount = m.Categories.Count(c => c.DeletedAt == null),
                     ProductCount = m.Products.Count(p => p.DeletedAt == null)
                 })
                 .FirstOrDefaultAsync();
            if (merchants == null)
                return Result<MerchantResponseDto>.Failure("Merchant not found.");
            return Result<MerchantResponseDto>.Success(merchants);
        }
        public async Task<Result> UpdateMerchantAsync(UpdateMerchantRequest request)
        {
            var existingMerchant = await _context.Merchants
                .FirstOrDefaultAsync(m => m.Id == request.Id && m.DeletedAt == null);

            if (existingMerchant == null)
                return Result.Failure("Merchant not found.");

            if (!string.IsNullOrWhiteSpace(request.Name))
                existingMerchant.Name = request.Name;

            if (!string.IsNullOrWhiteSpace(request.ContactEmail))
                existingMerchant.ContactEmail = request.ContactEmail;

            if (!string.IsNullOrWhiteSpace(request.Address))
                existingMerchant.Address = request.Address;

            if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
                existingMerchant.PhoneNumber = request.PhoneNumber;

            if (request.IsActive.HasValue)
                existingMerchant.IsActive = request.IsActive.Value;

            existingMerchant.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Result.Success();
        }
        public async Task<Result> RestoreMerchantAsync(Guid id, bool restoreAll = false)
        {
            var merchant = await _context.Merchants
                .IgnoreQueryFilters()
                .Include(m => m.Branches.Where(b => b.DeletedAt != null))
                .Include(m => m.Users.Where(u => u.DeletedAt != null))
                .Include(m => m.Products.Where(p => p.DeletedAt != null))
                .Include(m => m.Categories.Where(c => c.DeletedAt != null))
                .FirstOrDefaultAsync(m => m.Id == id);

            if (merchant == null)
                return Result.Failure("Merchant not found.");
            if (merchant.DeletedAt == null)
                return Result.Failure("Merchant is not deleted.");

            merchant.DeletedAt = null;
            merchant.IsActive = true;

            if (restoreAll)
            {
                foreach (var user in merchant.Users) { user.DeletedAt = null; user.IsActive = true; }
                foreach (var branch in merchant.Branches) { branch.DeletedAt = null; }
                foreach (var category in merchant.Categories) { category.DeletedAt = null; }
                foreach (var product in merchant.Products) { product.DeletedAt = null; }
            }

            await _context.SaveChangesAsync();
            return Result.Success();
        }
    }
}
