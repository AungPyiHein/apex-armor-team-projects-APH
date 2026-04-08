namespace MiniPos.Frontend.Pages.Sales;

class Order
{
    public Guid Id { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public List<OrderItem> OrderItems { get; set; } = new();
}

class OrderDetail
{
    public Guid Id { get; set; }
    public BranchDto? Branch { get; set; }
    public UserDto? ProcessedBy { get; set; }
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

class BranchDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
}

class UserDto
{
    public Guid Id { get; set; }
    public string? Username { get; set; }
}

class Merchant
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
}