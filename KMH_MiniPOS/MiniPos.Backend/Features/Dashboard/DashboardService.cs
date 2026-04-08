using Common;
using Database.EfAppDbContextModels;
using Microsoft.EntityFrameworkCore;
using MiniPos.Backend.Features.Orders;
using MiniPos.Backend.Features.Users;

namespace MiniPos.Backend.Features.Dashboard;

public interface IDashboardService
{
    Task<Result<DashboardResponse>> GetStats(DashboardRequest request);
}

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;

    public DashboardService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<DashboardResponse>> GetStats(DashboardRequest request)
    {
        const string errCode = "Dashboard.GetStats";
        try
        {
            var isMerchantOwner = await _db.Merchants
                .AnyAsync(m => m.Users
                    .Any(u => u.Id == request.ProcessedById && u.Role == nameof(UserRole.Merchant)));
            if (!isMerchantOwner)
                return Result<DashboardResponse>.Failure(new UnAuthorizedError(errCode,
                    "Unauthorized to access this merchant"));

            var today = DateTime.UtcNow.Date;
            var daysAgo = today.AddDays(-request.StatsDurationInDay);

            var orderQuery = _db.Orders
                .AsNoTracking()
                .AsQueryable()
                .Where(o => o.MerchantId == request.MerchantId);

            var merchantsCount = await _db.Merchants
                .Where(m => m.Users
                    .Any(u => u.Id == request.ProcessedById && u.Role == nameof(UserRole.Merchant))).CountAsync();

            var branchesCount = await _db.Branches.CountAsync(b => b.MerchantId == request.MerchantId);
            var revenue = await orderQuery.SumAsync(o => o.TotalAmount);
            var transactions = await orderQuery.CountAsync();
            var recentOrders = await orderQuery
                .OrderByDescending(o => o.OrderDate)
                .Take(10)
                .Select(o => new OrderListResponse
                {
                    Id = o.Id,
                    OrderDate = o.OrderDate,
                    TotalAmount = o.TotalAmount
                })
                .ToListAsync();

            var dailySales = new List<DailySaleDto>();
            for (var i = request.StatsDurationInDay; i > 0; i--)
            {
                var date = today.AddDays(-i);
                var dailyTotal = await orderQuery
                    .Where(o => o.OrderDate.Date == date)
                    .SumAsync(o => o.TotalAmount);

                dailySales.Add(new DailySaleDto
                {
                    Date = date,
                    TotalAmount = dailyTotal
                });
            }

            var response = new DashboardResponse
            {
                TotalRevenue = revenue,
                TotalTransactions = transactions,
                TotalMerchants = merchantsCount,
                TotalBranches = branchesCount,
                RecentOrders = recentOrders,
                DailySales = dailySales
            };

            return Result<DashboardResponse>.Success(response);
        }
        catch (Exception e)
        {
            return Result<DashboardResponse>.Failure(new InternalError("Dashboard.GetStats", e.Message));
        }
    }
}

public class DashboardRequest
{
    public required int StatsDurationInDay;
    public required Guid MerchantId;
    public required Guid ProcessedById;
}

public class DashboardResponse
{
    public decimal TotalRevenue { get; set; }
    public int TotalTransactions { get; set; }
    public int TotalMerchants { get; set; }
    public int TotalBranches { get; set; }
    public int TotalEmployees { get; set; }
    public List<OrderListResponse> RecentOrders { get; set; } = new();
    public List<DailySaleDto> DailySales { get; set; } = new();
}

public class DailySaleDto
{
    public DateTime Date { get; set; }
    public decimal TotalAmount { get; set; }
}