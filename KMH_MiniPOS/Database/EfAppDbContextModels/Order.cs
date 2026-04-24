using System;
using System.Collections.Generic;

namespace Database.EfAppDbContextModels;

public partial class Order
{
    public Guid Id { get; set; }

    public Guid BranchId { get; set; }

    public Guid? CustomerId { get; set; }

    public Guid? ProcessedById { get; set; }

    public DateTime OrderDate { get; set; }

    public decimal TotalAmount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public Guid MerchantId { get; set; }

    public virtual Branch Branch { get; set; } = null!;

    public virtual Customer? Customer { get; set; }

    public virtual Merchant Merchant { get; set; } = null!;

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual User? ProcessedBy { get; set; }
}
