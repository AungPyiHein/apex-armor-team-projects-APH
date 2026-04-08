using Common;
using Database.EfAppDbContextModels;
using Microsoft.EntityFrameworkCore;

namespace MiniPos.Backend.Features.Orders;

public interface IOrderService
{
    Task<Result<PagedResult<OrderListResponse>>> GetList(OrderListRequest request);
    Task<Result<OrderGetByIdResponse>> GetById(Guid id);
    Task<Result> Create(OrderCreateRequest request);
    Task<Result> Delete(Guid id);
}

public class OrderService : IOrderService
{
    private readonly AppDbContext _db;

    public OrderService(AppDbContext db)
    {
        _db = db;
    }

    private async Task<bool> IsMerchantOwner(Guid merchantId, Guid merchantAdminId)
    {
        return await _db.MerchantAdmins.AnyAsync(ma =>
            ma.MerchantId == merchantId && ma.UserId == merchantAdminId);
    }
    
    public async Task<Result<PagedResult<OrderListResponse>>> GetList(OrderListRequest request)
    {
        try
        {
            if (! await IsMerchantOwner(request.MerchantId, request.MerchantAdminId))
                return Result<PagedResult<OrderListResponse>>.Failure(new UnAuthorizedError("Orders.GetList",
                    "This user can not be accessible the orders of this merchant."));

            var query = _db.Orders
                .Where(o => o.MerchantId == request.MerchantId)
                .AsNoTracking()
                .AsQueryable();

            if (request.BranchId.HasValue)
                query = query.Where(o => o.BranchId == request.BranchId.Value);

            if (request.ProcessedById.HasValue)
                query = query.Where(o => o.ProcessedById == request.ProcessedById.Value);

            if (request.StartDate.HasValue)
                query = query.Where(o => o.OrderDate >= request.StartDate.Value);

            if (request.EndDate.HasValue)
                query = query.Where(o => o.OrderDate <= request.EndDate.Value);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                query = query.Where(o => o.OrderItems.Any(oi => oi.Product.Name.Contains(request.SearchTerm)));

            var skip = (request.PageNumber - 1) * request.PageSize;
            var take = request.PageSize;
            var totalCount = await query.CountAsync();
            Console.WriteLine($"total orders count: {totalCount} skip: {skip} take: {take}");
            var items = await query
                .OrderByDescending(order => order.OrderDate)
                .Skip(skip)
                .Take(take)
                .Select(order => new OrderListResponse
                {
                    Id = order.Id,
                    OrderDate = order.OrderDate,
                    TotalAmount = order.TotalAmount,
                    ItemCount = order.OrderItems.Count,
                    MerchantId = order.MerchantId,
                    BranchId = order.BranchId,
                    ProcessedById = order.ProcessedById,
                    OrderItems = order.OrderItems.Select(oi => new OrderItemDto
                    {
                        Id = oi.Id,
                        ProductId = oi.ProductId,
                        ProductName = oi.Product.Name,
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice,
                        SubTotal = oi.SubTotal
                    }).ToList()
                })
                .ToListAsync();

            var result = new PagedResult<OrderListResponse>(items, totalCount, request.PageNumber, request.PageSize);
            return Result<PagedResult<OrderListResponse>>.Success(result);
        }
        catch (Exception e)
        {
            return Result<PagedResult<OrderListResponse>>.Failure(new InternalError("Order.GetList", e.Message));
        }
    }

    public async Task<Result<OrderGetByIdResponse>> GetById(Guid id)
    {
        const string errCode = "Order.GetById";
        try
        {
            var order = await _db.Orders
                .AsNoTracking()
                .Include(o => o.Branch)
                .Include(o => o.ProcessedBy)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return Result<OrderGetByIdResponse>.Failure(new NotFoundError(errCode, "Order does not exist"));

            var dto = new OrderGetByIdResponse
            {
                Id = order.Id,
                OrderDate = order.OrderDate,
                TotalAmount = order.TotalAmount,
                Branch = new BranchDto
                {
                    Id = order.BranchId,
                    Name = order.Branch.Name
                },
                ProcessedBy = order.ProcessedBy != null
                    ? new UserDto
                    {
                        Id = order.ProcessedBy.Id,
                        Username = order.ProcessedBy.Username
                    }
                    : null,
                OrderItems = order.OrderItems.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    ProductId = oi.ProductId,
                    ProductName = oi.Product.Name,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    SubTotal = oi.SubTotal
                }).ToList()
            };

            return Result<OrderGetByIdResponse>.Success(dto);
        }
        catch (Exception e)
        {
            return Result<OrderGetByIdResponse>.Failure(new InternalError(errCode, e.Message));
        }
    }

    public async Task<Result> Create(OrderCreateRequest request)
    {
        const string errCode = "Order.Create";
        try
        {
            var userExist = await _db.Users.AnyAsync(u => u.Id == request.ProcessedById);
            if (!userExist) return Result.Failure(new NotFoundError(errCode, "User does not exist"));

            var branchExist =
                await _db.Branches.AnyAsync(b => b.Id == request.BranchId && b.MerchantId == request.MerchantId);
            if (!branchExist) return Result.Failure(new NotFoundError(errCode, "Branch does not exist"));

            var productIds = request.OrderedItems.Select(item => item.ProductId).Distinct().ToList();
            var inventories = await _db.BranchInventories
                .Include(bi => bi.Product)
                .Where(bi => bi.BranchId == request.BranchId && productIds.Contains(bi.ProductId))
                .ToListAsync();

            if (inventories.Count != productIds.Count)
                return Result.Failure(new NotFoundError(errCode,
                    $"One or more products do not exist in this branch's inventory"));

            var orderItems = new List<OrderItem>();
            decimal totalOrderAmount = 0;

            foreach (var orderedItem in request.OrderedItems)
            {
                var inventory = inventories.First(bi => bi.ProductId == orderedItem.ProductId);
                var product = inventory.Product;

                if (inventory.StockQuantity < orderedItem.Quantity)
                    return Result.Failure(new ValidationError(errCode, $"Insufficient stock for: {product.Name}"));

                inventory.StockQuantity -= orderedItem.Quantity;

                var calculatedSubTotal = orderedItem.Quantity * product.Price;
                totalOrderAmount += calculatedSubTotal;

                orderItems.Add(new OrderItem
                {
                    ProductId = product.Id,
                    Quantity = orderedItem.Quantity,
                    UnitPrice = product.Price,
                    SubTotal = calculatedSubTotal
                });
            }

            var order = new Order
            {
                MerchantId = request.MerchantId,
                BranchId = request.BranchId,
                ProcessedById = request.ProcessedById,
                OrderDate = DateTime.UtcNow,
                TotalAmount = totalOrderAmount,
                OrderItems = orderItems
            };

            await _db.Orders.AddAsync(order);
            await _db.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception e)
        {
            Console.WriteLine($"error creating order: {e.Message}");
            return Result.Failure(new InternalError(errCode, e.Message));
        }
    }

    public async Task<Result> Delete(Guid id)
    {
        const string errCode = "Order.Delete";
        try
        {
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id);
            if (order == null)
                return Result.Failure(new NotFoundError(errCode, "Order does not exist"));

            order.DeletedAt = DateTime.UtcNow;
            var result = await _db.SaveChangesAsync();

            return result > 0
                ? Result.Success()
                : Result.Failure(new InternalError(errCode, "Failed to delete order"));
        }
        catch (Exception e)
        {
            return Result.Failure(new InternalError(errCode, e.Message));
        }
    }
}

public class OrderListRequest : PaginationFilter
{
    public Guid MerchantId { get; set; }
    public Guid MerchantAdminId { get; set; }
    public Guid? ProcessedById { get; set; }
    public Guid? BranchId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? SearchTerm { get; set; }
}

public class OrderListResponse
{
    public Guid Id { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
    public Guid MerchantId { get; set; }
    public Guid BranchId { get; set; }
    public Guid? ProcessedById { get; set; }
    public List<OrderItemDto> OrderItems { get; set; } = new();

    public string? BranchName { get; set; }
}

public class BranchDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
}

public class UserDto
{
    public Guid Id { get; set; }
    public string? Username { get; set; }
}

public class OrderGetByIdResponse
{
    public Guid Id { get; set; }
    public BranchDto? Branch { get; set; }
    public UserDto? ProcessedBy { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public List<OrderItemDto> OrderItems { get; set; } = new();
}

public class OrderItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }
}

public class OrderCreateRequest
{
    public Guid MerchantId { get; set; }
    public Guid BranchId { get; set; }
    public Guid ProcessedById { get; set; }
    public List<OrderedItemRequest> OrderedItems { get; set; } = new();
}

public class OrderedItemRequest
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}