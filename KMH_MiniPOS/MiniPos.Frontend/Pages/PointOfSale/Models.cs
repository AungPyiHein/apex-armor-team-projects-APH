namespace MiniPos.Frontend.Pages.PointOfSale;

public class Merchant
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
}

public class Category
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
}

public class Branch
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Address { get; set; }
}

public class Product
{
    public Guid Id { get; set; }
    public Merchant? Merchant { get; set; }
    public Category? Category { get; set; }
    public string Name { get; set; } = null!;
    public string Sku { get; set; } = null!;
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class OrderItem
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }
    public Product? Product { get; set; }
}

class BranchInventory
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string Name { get; set; }
    public string Sku { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string? CategoryName { get; set; }
}