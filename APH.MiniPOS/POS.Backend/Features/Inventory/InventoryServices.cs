using Microsoft.EntityFrameworkCore;
using POS.Backend.Common;
using POS.data.Data;
using POS.data.Entities;

namespace POS.Backend.Features.Inventory
{
    public class InventoryResponseDto
    {
        public Guid Id { get; set; }
        public Guid BranchId { get; set; }
        public string BranchName { get; set; } = null!;
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public int StockQuantity { get; set; }
    }

    public class UpdateStockRequest
    {
        public Guid BranchId { get; set; }
        public Guid ProductId { get; set; }
        public int QuantityChange { get; set; }
    }

    public interface IInventoryServices
    {
        Task<Result<PagedResponse<InventoryResponseDto>>> GetBranchInventoryAsync(Guid branchId, PaginationFilter filter);
        Task<Result<IEnumerable<InventoryResponseDto>>> GetProductInventoryAsync(Guid productId);
        Task<Result<bool>> AdjustStockAsync(UpdateStockRequest request);
    }

    public class InventoryServices : IInventoryServices
    {
        private readonly AppDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public InventoryServices(AppDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result<PagedResponse<InventoryResponseDto>>> GetBranchInventoryAsync(Guid branchId, PaginationFilter filter)
        {
            var query = _context.BranchInventories
                .Include(i => i.Product)
                .Include(i => i.Branch)
                .Where(i => i.BranchId == branchId && i.DeletedAt == null)
                .AsQueryable();

            if (_currentUser.Role == POS.Shared.Models.UserRole.MerchantAdmin || _currentUser.Role == POS.Shared.Models.UserRole.Staff)
            {
                query = query.Where(i => i.Branch.MerchantId == _currentUser.MerchantId);
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                query = query.Where(i => EF.Functions.Like(i.Product.Name, $"%{filter.SearchTerm}%"));
            }

            var totalRecords = await query.CountAsync();

            var inventory = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(i => new InventoryResponseDto
                {
                    Id = i.Id,
                    BranchId = i.BranchId,
                    BranchName = i.Branch.Name,
                    ProductId = i.ProductId,
                    ProductName = i.Product.Name,
                    StockQuantity = i.StockQuantity
                })
                .ToListAsync();

            var pagedResponse = new PagedResponse<InventoryResponseDto>(inventory, totalRecords, filter.PageNumber, filter.PageSize);
            return Result<PagedResponse<InventoryResponseDto>>.Success(pagedResponse);
        }

        public async Task<Result<IEnumerable<InventoryResponseDto>>> GetProductInventoryAsync(Guid productId)
        {
            var branchQuery = _context.Branches.Where(b => b.DeletedAt == null).AsQueryable();

            if (_currentUser.Role == POS.Shared.Models.UserRole.MerchantAdmin || _currentUser.Role == POS.Shared.Models.UserRole.Staff)
            {
                branchQuery = branchQuery.Where(b => b.MerchantId == _currentUser.MerchantId);
            }

            var branches = await branchQuery.ToListAsync();

            var inventory = await _context.BranchInventories
                .Include(i => i.Product)
                .Where(i => i.ProductId == productId && i.DeletedAt == null)
                .ToDictionaryAsync(i => i.BranchId);

            var result = branches.Select(b => new InventoryResponseDto
            {
                Id = inventory.ContainsKey(b.Id) ? inventory[b.Id].Id : Guid.Empty,
                BranchId = b.Id,
                BranchName = b.Name,
                ProductId = productId,
                ProductName = inventory.ContainsKey(b.Id) ? inventory[b.Id].Product.Name : "N/A",
                StockQuantity = inventory.ContainsKey(b.Id) ? inventory[b.Id].StockQuantity : 0
            }).ToList();

            return Result<IEnumerable<InventoryResponseDto>>.Success(result);
        }

        public async Task<Result<bool>> AdjustStockAsync(UpdateStockRequest request)
        {
            // Staff can only adjust inventory for their own branch
            if (_currentUser.Role == POS.Shared.Models.UserRole.Staff)
            {
                if (_currentUser.BranchId == null || _currentUser.BranchId != request.BranchId)
                {
                    return Result<bool>.Failure("Staff can only adjust inventory for their own branch.");
                }
            }

            var inventory = await _context.BranchInventories
                .FirstOrDefaultAsync(i => i.BranchId == request.BranchId && i.ProductId == request.ProductId && i.DeletedAt == null);

            if (inventory == null)
            {
                // Create new inventory record if not exists
                inventory = new BranchInventory
                {
                    Id = Guid.NewGuid(),
                    BranchId = request.BranchId,
                    ProductId = request.ProductId,
                    StockQuantity = request.QuantityChange,
                    CreatedAt = DateTime.UtcNow
                };
                _context.BranchInventories.Add(inventory);
            }
            else
            {
                inventory.StockQuantity += request.QuantityChange;
                inventory.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Result<bool>.Success(true);
        }
    }
}
