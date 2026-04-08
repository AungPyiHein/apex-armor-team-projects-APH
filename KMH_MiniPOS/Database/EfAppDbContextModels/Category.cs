using System;
using System.Collections.Generic;

namespace Database.EfAppDbContextModels;

public partial class Category
{
    public Guid Id { get; set; }

    public Guid MerchantId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual Merchant Merchant { get; set; } = null!;

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
