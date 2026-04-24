using System;

namespace POS.Frontend.Models.Products;

public class UpdateProductRequest
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Barcode { get; set; }
    public decimal? Price { get; set; }
    public string? SKU { get; set; }
    public Guid? CategoryId { get; set; }
}
