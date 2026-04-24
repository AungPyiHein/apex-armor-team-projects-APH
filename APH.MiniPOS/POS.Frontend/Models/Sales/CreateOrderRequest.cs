using System;
using System.Collections.Generic;

namespace POS.Frontend.Models.Sales;

public class CreateOrderRequest
{
    public Guid BranchId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? ProcessedById { get; set; }
    public bool ApplyLoyalty { get; set; } = true;
    public List<CreateOrderItemRequest> Items { get; set; } = new();
}
