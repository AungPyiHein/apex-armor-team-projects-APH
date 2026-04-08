using System;
using System.Collections.Generic;

namespace Database.EfAppDbContextModels;

public partial class Customer
{
    public Guid Id { get; set; }

    public Guid MerchantId { get; set; }

    public string Name { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual Merchant Merchant { get; set; } = null!;

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
