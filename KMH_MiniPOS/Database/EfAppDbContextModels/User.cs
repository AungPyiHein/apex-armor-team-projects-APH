using System;
using System.Collections.Generic;

namespace Database.EfAppDbContextModels;

public partial class User
{
    public Guid Id { get; set; }

    public Guid? MerchantId { get; set; }

    public Guid? BranchId { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Role { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual Branch? Branch { get; set; }

    public virtual Merchant? Merchant { get; set; }

    public virtual ICollection<MerchantAdmin> MerchantAdmins { get; set; } = new List<MerchantAdmin>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
