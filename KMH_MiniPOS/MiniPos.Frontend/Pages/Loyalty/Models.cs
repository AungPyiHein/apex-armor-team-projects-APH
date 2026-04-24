using System.Text.Json.Serialization;

namespace MiniPos.Frontend.Pages.Loyalty;

public class RoyaltyDto
{
    public int CurrentPoint { get; set; }
    public int LifetimePoints { get; set; }
    public string Tier { get; set; } = string.Empty;
}

public class OrderDto
{
    public Guid Id { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AccountDetails
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid MerchantId { get; set; }
    public string MerchantName { get; set; } = null!;
    public List<OrderDto> Orders { get; set; } = [];
    public RoyaltyDto? Royalty { get; set; }
}

public sealed class Reward
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int PointCost { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; }
}

public sealed class ClaimRewardRequest
{
    public Guid CustomerId { get; set; }
    public string RewardId { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public sealed class PointHistory
{
    public int PointDelta { get; set; }
    public string EventKey { get; set; } = "";
    public string ReferenceId { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class ErrorResponse
{
    [JsonPropertyName("message")] public string Message { get; set; }
}