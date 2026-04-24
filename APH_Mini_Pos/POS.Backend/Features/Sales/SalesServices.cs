using Microsoft.EntityFrameworkCore;
using POS.Backend.Common;
using POS.Backend.Features.Inventory;
using POS.Backend.Features.Loyalty;
using POS.data.Data;
using POS.data.Entities;

namespace POS.Backend.Features.Sales
{
    public class CreateOrderRequest
    {
        public Guid BranchId { get; set; }
        public Guid? CustomerId { get; set; }
        public Guid? ProcessedById { get; set; }
        public bool ApplyLoyalty { get; set; } = true;
        public List<CreateOrderItemRequest> Items { get; set; } = new();
    }

    public class CreateOrderItemRequest
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class OrderResponseDto
    {
        public Guid Id { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public Guid? CustomerId { get; set; }
        public string CustomerName { get; set; } = "Walk-in Customer";
        public string BranchName { get; set; } = "Main Branch";
        public string Status { get; set; } = "Completed";
        public string CashierName { get; set; } = "Unknown";
        public List<OrderItemResponseDto> Items { get; set; } = new();
    }

    public class OrderItemResponseDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SubTotal { get; set; }
    }

    public interface ISalesServices
    {
        Task<Result<Guid>> CreateOrderAsync(CreateOrderRequest request);
        Task<Result<OrderResponseDto>> GetOrderByIdAsync(Guid id);
        Task<Result<PagedResponse<OrderResponseDto>>> GetAllOrdersAsync(PaginationFilter filter);
    }

    public class SalesServices : ISalesServices
    {
        private readonly AppDbContext _context;
        private readonly IInventoryServices _inventoryServices;
        private readonly ICurrentUserService _currentUser;
        private readonly ILoyaltyServices _loyaltyServices;
        private readonly ILogger<SalesServices> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public SalesServices(AppDbContext context, IInventoryServices inventoryServices, ICurrentUserService currentUser, ILoyaltyServices loyaltyServices, ILogger<SalesServices> logger, IServiceScopeFactory scopeFactory)
        {
            _context = context;
            _inventoryServices = inventoryServices;
            _currentUser = currentUser;
            _loyaltyServices = loyaltyServices;
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public async Task<Result<Guid>> CreateOrderAsync(CreateOrderRequest request)
        {
            if (_currentUser.Role == POS.Shared.Models.UserRole.Staff && request.BranchId != _currentUser.BranchId)
            {
                return Result<Guid>.Failure("Staff members can only create orders for their assigned branch.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var processedBy = request.ProcessedById ?? (_currentUser.IsAuthenticated && _currentUser.UserId != Guid.Empty ? _currentUser.UserId : null);

                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    BranchId = request.BranchId,
                    CustomerId = request.CustomerId,
                    ProcessedById = processedBy,
                    OrderDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    TotalAmount = 0
                };

                foreach (var itemRequest in request.Items)
                {
                    var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == itemRequest.ProductId && p.DeletedAt == null);
                    if (product == null) return Result<Guid>.Failure($"Product with ID {itemRequest.ProductId} not found or is deleted.");

                    var subTotal = product.Price * itemRequest.Quantity;
                    var orderItem = new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        ProductId = itemRequest.ProductId,
                        Quantity = itemRequest.Quantity,
                        UnitPrice = product.Price,
                        SubTotal = subTotal,
                        CreatedAt = DateTime.UtcNow
                    };

                    order.TotalAmount += subTotal;
                    order.OrderItems.Add(orderItem);

                    // Adjust inventory
                    var adjResult = await _inventoryServices.AdjustStockAsync(new UpdateStockRequest
                    {
                        BranchId = request.BranchId,
                        ProductId = itemRequest.ProductId,
                        QuantityChange = -itemRequest.Quantity
                    });

                    if (!adjResult.IsSuccess) return Result<Guid>.Failure($"Failed to adjust inventory for product {product.Name}: {adjResult.Error}");
                }

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Process Loyalty Event after successful transaction in background
                if (order.CustomerId.HasValue && request.ApplyLoyalty)
                {
                    var customer = await _context.Customers.FindAsync(order.CustomerId.Value);
                    if (customer != null)
                    {
                        // Look up merchant name via branch to derive a shop-specific EventKey
                        var branch = await _context.Branches
                            .Include(b => b.Merchant)
                            .FirstOrDefaultAsync(b => b.Id == request.BranchId);

                        var merchantName = branch?.Merchant?.Name ?? "DEFAULT";
                        // Derive the Loyalty Engine system ID as "APH_POS_{MERCHANTNAME}"
                        // e.g. merchant "Unique" → "APH_POS_UNIQUE" matching the registered tenant.
                        string? systemId = branch?.Merchant?.Name != null
                            ? "APH_POS_" + System.Text.RegularExpressions.Regex.Replace(branch.Merchant.Name.ToUpperInvariant(), @"[^A-Z0-9]", "_")
                            : null;
                        string? apiKey = null;
                        
                        string eventKey;
                        if (!string.IsNullOrWhiteSpace(systemId))
                        {
                            // If we have a shop-unique system ID, we use a standard PURCHASE event key 
                            // because the system ID already scopes it to that merchant.
                            eventKey = "PURCHASE";
                        }
                        else
                        {
                            // Sanitise: uppercase, replace spaces/special chars with underscore
                            var safeKey = System.Text.RegularExpressions.Regex
                                .Replace(merchantName.ToUpperInvariant(), @"[^A-Z0-9]", "_");
                            eventKey = $"{safeKey}_PURCHASE";
                        }

                        var cId = customer.Id;
                        var cName = customer.Name;
                        var cEmail = customer.Email;
                        var cPhone = customer.PhoneNumber;
                        var oAmount = order.TotalAmount;
                        var oId = order.Id;

                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                using var scope = _scopeFactory.CreateScope();
                                var loyaltyService = scope.ServiceProvider.GetRequiredService<ILoyaltyServices>();
                                await loyaltyService.ProcessSaleEventAsync(cId, cName, oAmount, oId, cEmail, cPhone, eventKey, systemId, apiKey);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to process loyalty event for order {OrderId}", oId);
                            }
                        });
                    }
                }

                return Result<Guid>.Success(order.Id);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Result<Guid>.Failure($"An error occurred while creating order: {ex.Message}");
            }
        }

        public async Task<Result<OrderResponseDto>> GetOrderByIdAsync(Guid id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Include(o => o.Customer)
                .Include(o => o.Branch)
                .Include(o => o.ProcessedBy)
                .Where(o => o.Id == id && o.DeletedAt == null)
                .Select(o => new OrderResponseDto
                {
                    Id = o.Id,
                    OrderDate = o.OrderDate,
                    TotalAmount = o.TotalAmount,
                    CustomerId = o.CustomerId,
                    CustomerName = o.Customer != null ? o.Customer.Name : "Walk-in Customer",
                    BranchName = o.Branch != null ? o.Branch.Name : "Main Branch",
                    Status = "Completed",
                    CashierName = o.ProcessedBy != null ? (!string.IsNullOrWhiteSpace(o.ProcessedBy.FullName) ? o.ProcessedBy.FullName : o.ProcessedBy.Username) : "Unknown",
                    Items = o.OrderItems.Select(oi => new OrderItemResponseDto
                    {
                        ProductId = oi.ProductId,
                        ProductName = oi.Product.Name,
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice,
                        SubTotal = oi.SubTotal
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (order == null) return Result<OrderResponseDto>.Failure("Order not found.");

            return Result<OrderResponseDto>.Success(order);
        }

        public async Task<Result<PagedResponse<OrderResponseDto>>> GetAllOrdersAsync(PaginationFilter filter)
        {
            var query = _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Include(o => o.Customer)
                .Include(o => o.Branch)
                .Include(o => o.ProcessedBy)
                .Where(o => o.DeletedAt == null)
                .OrderByDescending(o => o.OrderDate)
                .AsQueryable();

            if (_currentUser.Role == POS.Shared.Models.UserRole.MerchantAdmin)
            {
                query = query.Where(o => o.Branch != null && o.Branch.MerchantId == _currentUser.MerchantId);
            }
            else if (_currentUser.Role == POS.Shared.Models.UserRole.Staff)
            {
                query = query.Where(o => o.BranchId == _currentUser.BranchId);
            }

            if (filter.ProcessedById.HasValue)
            {
                query = query.Where(o => o.ProcessedById == filter.ProcessedById.Value);
            }

            if (filter.BranchId.HasValue)
            {
                query = query.Where(o => o.BranchId == filter.BranchId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                query = query.Where(o => (o.Customer != null && EF.Functions.Like(o.Customer.Name, $"%{filter.SearchTerm}%")) ||
                                         EF.Functions.Like(o.Id.ToString(), $"%{filter.SearchTerm}%"));
            }

            if (filter.StartDate.HasValue)
            {
                var startDate = filter.StartDate.Value.Date;
                query = query.Where(o => o.OrderDate >= startDate);
            }
            
            if (filter.EndDate.HasValue)
            {
                // Ensure the end date includes the entire day (up to 23:59:59)
                var endDate = filter.EndDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(o => o.OrderDate <= endDate);
            }

            var totalRecords = await query.CountAsync();

            var orders = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(o => new OrderResponseDto
                {
                    Id = o.Id,
                    OrderDate = o.OrderDate,
                    TotalAmount = o.TotalAmount,
                    CustomerId = o.CustomerId,
                    CustomerName = o.Customer != null ? o.Customer.Name : "Walk-in Customer",
                    BranchName = o.Branch != null ? o.Branch.Name : "Main Branch",
                    Status = "Completed",
                    CashierName = o.ProcessedBy != null ? (!string.IsNullOrWhiteSpace(o.ProcessedBy.FullName) ? o.ProcessedBy.FullName : o.ProcessedBy.Username) : "Unknown",
                    Items = o.OrderItems.Select(oi => new OrderItemResponseDto
                    {
                        ProductId = oi.ProductId,
                        ProductName = oi.Product.Name,
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice,
                        SubTotal = oi.SubTotal
                    }).ToList()
                })
                .ToListAsync();

            var pagedResponse = new PagedResponse<OrderResponseDto>(orders, totalRecords, filter.PageNumber, filter.PageSize);
            return Result<PagedResponse<OrderResponseDto>>.Success(pagedResponse);
        }
    }
}
