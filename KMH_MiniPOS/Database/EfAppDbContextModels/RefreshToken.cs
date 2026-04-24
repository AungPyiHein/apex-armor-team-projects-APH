using System;
using System.Collections.Generic;

namespace Database.EfAppDbContextModels;

public partial class RefreshToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Token { get; set; } = null!;

    public DateTimeOffset? ExpiresAt { get; set; }

    public bool IsRevoked { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? ReplacedByTokenHash { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
