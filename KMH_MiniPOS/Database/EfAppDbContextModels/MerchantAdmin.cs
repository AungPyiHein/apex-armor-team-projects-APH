using System;
using System.Collections.Generic;

namespace Database.EfAppDbContextModels;

public partial class MerchantAdmin
{
    public Guid UserId { get; set; }

    public Guid MerchantId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public virtual Merchant Merchant { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
