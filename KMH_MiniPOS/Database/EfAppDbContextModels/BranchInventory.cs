using System;
using System.Collections.Generic;

namespace Database.EfAppDbContextModels;

public partial class BranchInventory
{
    public Guid Id { get; set; }

    public Guid BranchId { get; set; }

    public Guid ProductId { get; set; }

    public int StockQuantity { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual Branch Branch { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
