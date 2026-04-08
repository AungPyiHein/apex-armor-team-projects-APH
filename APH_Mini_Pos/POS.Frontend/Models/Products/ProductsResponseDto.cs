using System;

namespace POS.Frontend.Models.Products;

public class ProductsResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Barcode { get; set; }
    public decimal Price { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryDescription { get; set; } = string.Empty;
    public string MerchantName { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public Guid MerchantId { get; set; }
}
