namespace MiniPos.Frontend.Pages.Dashboard;

class Merchant
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
}

class DashboardRequest
{
    public required int StatsDurationInDay;
    public required Guid MerchantId;

    public string ToQuery()
    {
        return $"merchantId={MerchantId}&days={StatsDurationInDay}";
    }
}

class DashboardResponse
{
    public decimal TotalRevenue { get; set; }
    public int TotalTransactions { get; set; }
    public int TotalMerchants { get; set; }
    public int TotalBranches { get; set; }
    public int TotalEmployees { get; set; }
    public List<Order> RecentOrders { get; set; } = new();
    public List<DailySale> DailySales { get; set; } = new();
}

class DailySale
{
    public DateTime Date { get; set; }
    public decimal TotalAmount { get; set; }
}

class Order
{
    public Guid Id { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public List<OrderItem> OrderItems { get; set; } = new();
}

class OrderItem
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }
}