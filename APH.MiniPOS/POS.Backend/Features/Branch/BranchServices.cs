using Microsoft.EntityFrameworkCore;
using POS.Backend.Common;
using POS.data.Data;


namespace POS.Backend.Features.Branch
{
    public class CreateBranchRequest
    {
        public string Name { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public Guid MerchantId { get; set; }
    }
    public class UpdateBranchRequest
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string? PhoneNumber { get; set; }
    }
    public class BranchResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public int ActiveUsersCount { get; set; }
        public string MerchantName { get; set; } = null!;
    }
    public interface IBranchServices
    {
        Task<Result<Guid>> CreateBranchAsync(CreateBranchRequest request);
        Task<Result> UpdateBranchAsync(UpdateBranchRequest request);
        Task<Result> DeleteBranchAsync(Guid id);
        Task<Result> RestoreBranchAsync(Guid id);
        Task<Result<PagedResponse<BranchResponse>>> GetBranchesByMerchantIdAsync(Guid merchantId, PaginationFilter filter);
        Task<Result<BranchResponse>> GetBranchByIdAsync(Guid id);
        Task<Result<PagedResponse<BranchResponse>>> GetAllBranchesAsync(PaginationFilter filter);
    }
    public class BranchServices : IBranchServices
    {
        private readonly AppDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public BranchServices(AppDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result<Guid>> CreateBranchAsync(CreateBranchRequest request)
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
            if (!merchantExists) return Result<Guid>.Failure("Merchant not found.");

            var nameExists = await _context.Branches.AnyAsync(b =>
                b.MerchantId == request.MerchantId && b.Name == request.Name && b.DeletedAt == null);

            if (nameExists) return Result<Guid>.Failure("A branch with this name already exists.");

            var branch = new data.Entities.Branch
            {
                Id = Guid.NewGuid(),
                MerchantId = request.MerchantId,
                Name = request.Name,
                Address = request.Address,
                PhoneNumber = request.PhoneNumber,
                CreatedAt = DateTime.UtcNow
            };

            _context.Branches.Add(branch);
            await _context.SaveChangesAsync();

            return Result<Guid>.Success(branch.Id);
        }


        public async Task<Result> UpdateBranchAsync(UpdateBranchRequest request)
        {
            var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == request.Id && b.DeletedAt == null);
            if (branch == null) return Result.Failure("Branch not found.");
            var nameExists = await _context.Branches.AnyAsync(b =>
                b.MerchantId == branch.MerchantId && b.Name == request.Name && b.Id != request.Id && b.DeletedAt == null);
            if (nameExists) return Result.Failure("A branch with this name already exists.");
            branch.Name = request.Name;
            branch.Address = request.Address;
            branch.PhoneNumber = request.PhoneNumber;
            branch.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Result.Success();
        }
        public async Task<Result<PagedResponse<BranchResponse>>> GetBranchesByMerchantIdAsync(Guid merchantId, PaginationFilter filter)
        {
            var query = _context.Branches
                .Where(b => b.MerchantId == merchantId && b.DeletedAt == null)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                query = query.Where(b => EF.Functions.Like(b.Name, $"%{filter.SearchTerm}%") ||
                                         EF.Functions.Like(b.Address, $"%{filter.SearchTerm}%"));
            }

            var totalRecords = await query.CountAsync();

            var branches = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(b => new BranchResponse
                {
                    Id = b.Id,
                    Name = b.Name,
                    Address = b.Address,
                    PhoneNumber = b.PhoneNumber,
                    ActiveUsersCount = b.Users.Count(u => u.DeletedAt == null),
                    MerchantName = b.Merchant.Name
                })
                .ToListAsync();

            var pagedResponse = new PagedResponse<BranchResponse>(branches, totalRecords, filter.PageNumber, filter.PageSize);
            return Result<PagedResponse<BranchResponse>>.Success(pagedResponse);
        }

        public async Task<Result<BranchResponse>> GetBranchByIdAsync(Guid id)
        {
            var branch = await _context.Branches
                .Where(b => b.Id == id && b.DeletedAt == null)
                .Select(b => new BranchResponse
                {
                    Id = b.Id,
                    Name = b.Name,
                    Address = b.Address,
                    PhoneNumber = b.PhoneNumber,
                    ActiveUsersCount = b.Users.Count(u => u.DeletedAt == null),
                    MerchantName = b.Merchant.Name
                })
                .FirstOrDefaultAsync();

            if (branch == null) return Result<BranchResponse>.Failure("Branch not found.");
            return Result<BranchResponse>.Success(branch);
        }

        public async Task<Result<PagedResponse<BranchResponse>>> GetAllBranchesAsync(PaginationFilter filter)
        {
            var query = _context.Branches
                .Where(b => b.DeletedAt == null)
                .AsQueryable();

            if (_currentUser.Role == POS.Shared.Models.UserRole.MerchantAdmin || _currentUser.Role == POS.Shared.Models.UserRole.Staff)
            {
                query = query.Where(b => b.MerchantId == _currentUser.MerchantId);
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                query = query.Where(b => EF.Functions.Like(b.Name, $"%{filter.SearchTerm}%") ||
                                         EF.Functions.Like(b.Address, $"%{filter.SearchTerm}%"));
            }

            var totalRecords = await query.CountAsync();

            var branches = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(b => new BranchResponse
                {
                    Id = b.Id,
                    Name = b.Name,
                    Address = b.Address,
                    PhoneNumber = b.PhoneNumber,
                    ActiveUsersCount = b.Users.Count(u => u.DeletedAt == null),
                    MerchantName = b.Merchant.Name
                })
                .ToListAsync();

            var pagedResponse = new PagedResponse<BranchResponse>(branches, totalRecords, filter.PageNumber, filter.PageSize);
            return Result<PagedResponse<BranchResponse>>.Success(pagedResponse);
        }
        public async Task<Result> DeleteBranchAsync(Guid id)
        {
            var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == id && b.DeletedAt == null);
            if (branch == null) return Result.Failure("Branch not found.");
            branch.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Result.Success();
        }
        public async Task<Result> RestoreBranchAsync(Guid id)
        {
            var branch = await _context.Branches
                .IgnoreQueryFilters()
                .Include(b => b.Merchant)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (branch == null) return Result.Failure("Branch not found.");
            if (branch.DeletedAt == null) return Result.Failure("Branch is not deleted.");

            if (branch.Merchant != null && branch.Merchant.DeletedAt != null)
            {
                return Result.Failure("Cannot restore this branch because the Merchant is deleted.");
            }

            branch.DeletedAt = null;
            await _context.SaveChangesAsync();
            return Result.Success();
        }
    }
}
