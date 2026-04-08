namespace POS.Frontend.Models.Sales;

public class OrderResponseDto
{
    public Guid Id { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public Guid? CustomerId { get; set; }
    public string CustomerName { get; set; } = "Walk-in Customer";
    public string BranchName { get; set; } = "Main Branch";
    public string Status { get; set; } = "Completed";
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
