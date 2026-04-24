using System;

namespace POS.Frontend.Models.Inventory;

public class InventoryResponseDto
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = null!;
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public int StockQuantity { get; set; }
}

public class UpdateStockRequest
{
    public Guid BranchId { get; set; }
    public Guid ProductId { get; set; }
    public int QuantityChange { get; set; }
}
