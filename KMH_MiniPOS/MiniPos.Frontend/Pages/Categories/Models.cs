namespace MiniPos.Frontend.Pages.Categories;

public class Merchant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
}

public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
}

public class CategoryListResponse
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public Guid MerchantId { get; set; }
    public string? MerchantName { get; set; }
    public int ProductCount { get; set; }
}

public class CategoryGetByIdResponse
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public Merchant? Merchant { get; set; }
    public List<Product> Products { get; set; } = new List<Product>();
    public DateTime CreatedAt { get; set; }
}
