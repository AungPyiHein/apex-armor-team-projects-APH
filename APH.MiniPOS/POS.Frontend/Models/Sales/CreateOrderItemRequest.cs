using System;

namespace POS.Frontend.Models.Sales;

public class CreateOrderItemRequest
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}
