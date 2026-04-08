namespace MiniPos.Frontend.Pages.Products;

class Merchant
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
}

class Category
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
}

class Branch
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
}

class Product
{
    public Guid Id { get; set; }
    public string? Name { get; set; } 
    public string? Sku { get; set; } 
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; }
    public Category? Category { get; set; }
    public Merchant? Merchant { get; set; }
}

class ProductCreateRequest
{
    public Guid MerchantId { get; set; }
    public Guid CategoryId { get; set; }
    public string? Name { get; set; }
    public string? Sku { get; set; }
    public decimal Price { get; set; }
}

public class ProductUpdateRequest
{
    public Guid? CategoryId { get; set; }
    public string? Name { get; set; }
    public string? Sku { get; set; }
    public decimal? Price { get; set; }
}