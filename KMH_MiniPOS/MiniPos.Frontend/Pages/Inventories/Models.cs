namespace MiniPos.Frontend.Pages.Inventories;

public class Category
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
}

public class Merchant
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
}

public class Branch {
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
}

public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Sku { get; set; } = null!;
    public decimal Price { get; set; }
    public string? CategoryName { get; set; }
}

public class BranchInventoryListResponse
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string Name { get; set; }
    public string Sku { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string? CategoryName { get; set; }
}

public class BranchInventoryCreateRequest
{
    public Guid MerchantId { get; set; }
    public Guid BranchId { get; set; }
    public Guid ProductId { get; set; }
    public int StockQuantity { get; set; }
}

public class BranchInventoryUpdateRequest
{
    public decimal? Price { get; set; }
    public int? StockQuantity { get; set; }
}

public class BranchInventoryGetByIdResponse
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Sku { get; set; } = null!;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string? CategoryName { get; set; }
}
