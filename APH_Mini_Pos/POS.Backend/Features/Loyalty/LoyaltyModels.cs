using System.Text.Json.Serialization;

namespace POS.Backend.Features.Loyalty
{
    public class LoyaltyEventProcessRequest
    {
        [JsonPropertyName("externalUserId")]
        public string ExternalUserId { get; set; } = string.Empty;

        [JsonPropertyName("eventKey")]
        public string EventKey { get; set; } = string.Empty;

        [JsonPropertyName("eventValue")]
        public double EventValue { get; set; }

        [JsonPropertyName("referenceId")]
        public string? ReferenceId { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("mobile")]
        public string? Mobile { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("metadata")]
        public object? Metadata { get; set; }
    }

    public class LoyaltyAccountResponse
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("accountId")]
        public Guid AccountId { get; set; }

        [JsonPropertyName("systemId")]
        public string SystemId { get; set; } = string.Empty;

        [JsonPropertyName("externalUserId")]
        public string ExternalUserId { get; set; } = string.Empty;

        [JsonPropertyName("tier")]
        public string? Tier { get; set; }

        [JsonPropertyName("totalPointsEarned")]
        public decimal TotalPointsEarned { get; set; }

        [JsonPropertyName("lifetimePoints")]
        public decimal LifetimePoints { get; set; }

        [JsonPropertyName("totalPointsSpent")]
        public decimal TotalPointsSpent { get; set; }

        [JsonPropertyName("currentBalance")]
        public decimal CurrentBalance { get; set; }

        [JsonPropertyName("mobile")]
        public string? Mobile { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }
    }

    public class LoyaltyReward
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("pointCost")]
        public int PointCost { get; set; }

        [JsonPropertyName("stockQuantity")]
        public int? StockQuantity { get; set; }

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }
    }

    public class LoyaltyRuleDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("systemId")]
        public string SystemId { get; set; } = string.Empty;

        [JsonPropertyName("eventKey")]
        public string EventKey { get; set; } = string.Empty;

        [JsonPropertyName("calculationType")]
        public string CalculationType { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public decimal Value { get; set; }

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }
    }

    public class LoyaltyHistoryDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("pointDelta")]
        public decimal PointDelta { get; set; }

        [JsonPropertyName("eventKey")]
        public string? EventKey { get; set; }

        [JsonPropertyName("referenceId")]
        public string? ReferenceId { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("rewardId")]
        public Guid? RewardId { get; set; }

        [JsonPropertyName("rewardName")]
        public string? RewardName { get; set; }

        [JsonPropertyName("redemptionStatus")]
        public string? RedemptionStatus { get; set; }

        [JsonPropertyName("redeemedAt")]
        public DateTime? RedeemedAt { get; set; }
    }

    public class LoyaltyAdminStatsResponse
    {
        [JsonPropertyName("systemsCount")]
        public int SystemsCount { get; set; }

        [JsonPropertyName("rulesCount")]
        public int RulesCount { get; set; }

        [JsonPropertyName("pendingRedemptions")]
        public int PendingRedemptions { get; set; }
    }

    public class RedemptionHistoryItemDto
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("systemId")]
        public string SystemId { get; set; } = string.Empty;

        [JsonPropertyName("externalUserId")]
        public string ExternalUserId { get; set; } = string.Empty;

        [JsonPropertyName("customerName")]
        public string? CustomerName { get; set; }

        [JsonPropertyName("rewardName")]
        public string RewardName { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("pointCost")]
        public int PointCost { get; set; }

        [JsonPropertyName("redeemedAt")]
        public DateTime? RedeemedAt { get; set; }
    }

    public class PagedRedemptionHistoryResponse
    {
        [JsonPropertyName("items")]
        public List<RedemptionHistoryItemDto> Items { get; set; } = new();

        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }

        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; }
    }

    public class PagedLedgerHistoryResponse
    {
        [JsonPropertyName("items")]
        public List<LoyaltyHistoryDto> Items { get; set; } = new();

        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }

        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; }
    }
}
