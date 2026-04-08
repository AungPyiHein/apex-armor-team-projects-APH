using System;
using System.Collections.Generic;

namespace Database.EfAppDbContextModels;

public partial class Product
{
    public Guid Id { get; set; }

    public Guid MerchantId { get; set; }

    public Guid CategoryId { get; set; }

    public string Name { get; set; } = null!;

    public string Sku { get; set; } = null!;

    public decimal Price { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<BranchInventory> BranchInventories { get; set; } = new List<BranchInventory>();

    public virtual Category Category { get; set; } = null!;

    public virtual Merchant Merchant { get; set; } = null!;

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
}
