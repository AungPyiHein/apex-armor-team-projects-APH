using System.Text.Json.Serialization;
using Common;

namespace MiniPos.Backend.Features.Loyalties;

public class Account
{
    public Guid AccountId { get; set; }
    public string ExternalUserId { get; set; } = string.Empty;
    public int CurrentBalance { get; set; }
    public int LifetimePoints { get; set; }
    public string Tier { get; set; } = string.Empty;
}

public class Reward
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int PointCost { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; }
}

public class GetAccountsRequest : PaginationFilter
{
}

public class GetAccountRequest
{
    public Guid MerchantAdminId { get; set; }
    public Guid MerchantId { get; set; }
    public Guid CustomerId { get; set; }
}

public class CreateEventRequest
{
    public string ExternalUserId { get; set; } = string.Empty;
    public decimal EventValue { get; set; }
    public string EventKey { get; set; } = string.Empty;
    public string ReferenceId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Mobile { get; set; } = string.Empty;
    public string? Email { get; set; } = string.Empty;
}

public class CreateEventResponse
{
    public int CurrentBalance { get; set; }
}

public class GetRewardsRequest : PaginationFilter
{
}

public class ClaimRewardRequest
{
    public Guid CustomerId { get; set; }
    public string ExternalUserId { get; set; } = string.Empty;
    public string RewardId { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public class PointHistory
{
    public int PointDelta { get; set; }
    public string EventKey { get; set; } = string.Empty;
    public string ReferenceId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class GetPointHistoriesRequest : PaginationFilter
{
}

public class ErrorResponse
{
    [JsonPropertyName("error")] public string Error { get; set; }
}