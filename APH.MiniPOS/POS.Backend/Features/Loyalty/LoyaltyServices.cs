using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using POS.Backend.Common;

namespace POS.Backend.Features.Loyalty
{
    public interface ILoyaltyServices
    {
        Task<Result<bool>> ProcessSaleEventAsync(Guid customerId, string customerName, decimal amount, Guid orderId, string? email = null, string? mobile = null, string? eventKey = null, string? systemId = null, string? apiKey = null);
        Task<Result<LoyaltyAccountResponse>> GetCustomerLoyaltyAsync(Guid customerId, string? systemId = null, string? apiKey = null);
        Task<Result<List<LoyaltyReward>>> GetActiveRewardsAsync(string? systemId = null, string? apiKey = null);
        Task<Result<bool>> ClaimRewardAsync(Guid customerId, Guid rewardId, string? notes = null, string? systemId = null, string? apiKey = null);
        Task<Result<List<LoyaltyRuleDto>>> GetActiveRulesAsync(string? systemId = null, string? apiKey = null);
        Task<Result<List<LoyaltyHistoryDto>>> GetCustomerHistoryAsync(Guid customerId, string? systemId = null, string? apiKey = null);
        Task<Result<LoyaltyAdminStatsResponse>> GetAdminStatsAsync(string? systemId = null, string? apiKey = null);
        Task<Result<PagedRedemptionHistoryResponse>> GetRedemptionHistoryAsync(int page = 1, int pageSize = 10, string? status = null, string? searchTerm = null, string? systemId = null, string? apiKey = null);
        Task<Result<PagedLedgerHistoryResponse>> GetGlobalLedgerAsync(int page = 1, int pageSize = 10, string? searchTerm = null, string? systemId = null, string? apiKey = null);
        Task<Result<bool>> FulfillRedemptionAsync(Guid redemptionId, string? systemId = null, string? apiKey = null);
    }

    public class LoyaltySettings
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string SystemId { get; set; } = string.Empty;
        public string? ApiKey { get; set; }
        public string DefaultEventKey { get; set; } = "PURCHASE";
    }

    public class LoyaltyServices : ILoyaltyServices
    {
        private readonly HttpClient _httpClient;
        private readonly LoyaltySettings _settings;
        private readonly ILogger<LoyaltyServices> _logger;

        public LoyaltyServices(HttpClient httpClient, IOptions<LoyaltySettings> settings, ILogger<LoyaltyServices> logger)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;

            if (string.IsNullOrEmpty(_httpClient.BaseAddress?.ToString()) && !string.IsNullOrEmpty(_settings.BaseUrl))
            {
                _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
            }
        }

        private async Task<HttpResponseMessage> SendWithHeadersAsync(HttpMethod method, string url, object? body = null, string? systemId = null, string? apiKey = null)
        {
            var request = new HttpRequestMessage(method, url);
            
            var targetSystemId = !string.IsNullOrWhiteSpace(systemId) ? systemId : _settings.SystemId;
            var targetApiKey = !string.IsNullOrWhiteSpace(apiKey) ? apiKey : _settings.ApiKey;

            if (!string.IsNullOrWhiteSpace(targetSystemId))
                request.Headers.Add("x-system-id", targetSystemId);
            
            if (!string.IsNullOrWhiteSpace(targetApiKey))
                request.Headers.Add("x-api-key", targetApiKey);

            if (body != null)
            {
                request.Content = JsonContent.Create(body);
            }

            return await _httpClient.SendAsync(request);
        }

        public async Task<Result<bool>> ProcessSaleEventAsync(Guid customerId, string customerName, decimal amount, Guid orderId, string? email = null, string? mobile = null, string? eventKey = null, string? systemId = null, string? apiKey = null)
        {
            try
            {
                var eventProcessRequest = new LoyaltyEventProcessRequest
                {
                    ExternalUserId = customerId.ToString(),
                    EventKey = eventKey ?? _settings.DefaultEventKey,
                    EventValue = (double)amount,
                    ReferenceId = orderId.ToString(),
                    Description = $"Purchase by {customerName} - Order {orderId}",
                    Email = email,
                    Mobile = mobile
                };

                var response = await SendWithHeadersAsync(HttpMethod.Post, "api/v1/events/process", eventProcessRequest, systemId, apiKey);
                
                if (response.IsSuccessStatusCode)
                {
                    return Result<bool>.Success(true);
                }

                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Loyalty API Error processing event: {Status} - {Error}", response.StatusCode, error);
                return Result<bool>.Failure($"Loyalty API error: {response.StatusCode} - {error}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process loyalty event");
                return Result<bool>.Failure($"Failed to connect to Loyalty API: {ex.Message}");
            }
        }

        public async Task<Result<LoyaltyAccountResponse>> GetCustomerLoyaltyAsync(Guid customerId, string? systemId = null, string? apiKey = null)
        {
            try
            {
                var targetSystemId = !string.IsNullOrWhiteSpace(systemId) ? systemId : _settings.SystemId;
                var response = await SendWithHeadersAsync(HttpMethod.Get, $"api/v1/accounts/lookup/{targetSystemId}/{customerId}", null, systemId, apiKey);

                if (response.IsSuccessStatusCode)
                {
                    var account = await response.Content.ReadFromJsonAsync<LoyaltyAccountResponse>();
                    return account != null ? Result<LoyaltyAccountResponse>.Success(account) : Result<LoyaltyAccountResponse>.Failure("Empty response");
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return Result<LoyaltyAccountResponse>.Failure("Customer loyalty account not found");

                return Result<LoyaltyAccountResponse>.Failure($"Loyalty API error: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch loyalty account");
                return Result<LoyaltyAccountResponse>.Failure($"Failed to connect to Loyalty API: {ex.Message}");
            }
        }

        public async Task<Result<List<LoyaltyReward>>> GetActiveRewardsAsync(string? systemId = null, string? apiKey = null)
        {
            try
            {
                var targetSystemId = !string.IsNullOrWhiteSpace(systemId) ? systemId : _settings.SystemId;
                var response = await SendWithHeadersAsync(HttpMethod.Get, $"api/v1/rewards/active/{targetSystemId}", null, systemId, apiKey);

                if (response.IsSuccessStatusCode)
                {
                    var rewards = await response.Content.ReadFromJsonAsync<List<LoyaltyReward>>();
                    return rewards != null ? Result<List<LoyaltyReward>>.Success(rewards) : Result<List<LoyaltyReward>>.Failure("Empty response");
                }

                return Result<List<LoyaltyReward>>.Failure($"Loyalty API error: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch active rewards");
                return Result<List<LoyaltyReward>>.Failure($"Failed to connect to Loyalty API: {ex.Message}");
            }
        }

        public async Task<Result<bool>> ClaimRewardAsync(Guid customerId, Guid rewardId, string? notes = null, string? systemId = null, string? apiKey = null)
        {
            try
            {
                var body = new
                {
                    externalUserId = customerId.ToString(),
                    rewardId = rewardId,
                    notes = notes
                };

                var claimResponse = await SendWithHeadersAsync(HttpMethod.Post, "api/v1/redemption/claim", body, systemId, apiKey);

                if (!claimResponse.IsSuccessStatusCode)
                {
                    var error = await claimResponse.Content.ReadAsStringAsync();
                    return Result<bool>.Failure($"Loyalty claim error: {error}");
                }

                // Manual workflow: keep redemption pending until merchant/admin fulfills it.
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"Failed to connect to Loyalty API: {ex.Message}");
            }
        }

        public async Task<Result<bool>> FulfillRedemptionAsync(Guid redemptionId, string? systemId = null, string? apiKey = null)
        {
            try
            {
                var fulfilled = await TryFulfillRedemptionAsync(redemptionId, systemId, apiKey);
                return fulfilled
                    ? Result<bool>.Success(true)
                    : Result<bool>.Failure("Failed to fulfill redemption.");
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"Failed to connect to Loyalty API: {ex.Message}");
            }
        }

        private static bool TryGetGuidFromProperty(System.Text.Json.JsonElement element, string propertyName, out Guid guid)
        {
            guid = Guid.Empty;
            if (!element.TryGetProperty(propertyName, out var prop))
            {
                return false;
            }

            if (prop.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                return Guid.TryParse(prop.GetString(), out guid);
            }

            return prop.TryGetGuid(out guid);
        }

        private async Task<Guid?> TryReadRedemptionIdFromClaimAsync(HttpResponseMessage claimResponse)
        {
            try
            {
                var root = await claimResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();

                if (TryGetGuidFromProperty(root, "id", out var rootId))
                {
                    return rootId;
                }

                if (root.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    if (root.TryGetProperty("data", out var data))
                    {
                        if (TryGetGuidFromProperty(data, "id", out var dataId))
                        {
                            return dataId;
                        }
                    }

                    if (root.TryGetProperty("redemptionId", out var redemptionIdProp))
                    {
                        if (redemptionIdProp.ValueKind == System.Text.Json.JsonValueKind.String && Guid.TryParse(redemptionIdProp.GetString(), out var redId))
                        {
                            return redId;
                        }

                        if (redemptionIdProp.TryGetGuid(out redId))
                        {
                            return redId;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Claim response parsed without redemption id");
            }

            return null;
        }

        private async Task<bool> TryFulfillRedemptionAsync(Guid redemptionId, string? systemId, string? apiKey)
        {
            var putPayload = new object[]
            {
                new { status = "Fulfilled" },
                new { Status = "Fulfilled" }
            };

            foreach (var payload in putPayload)
            {
                var putResponse = await SendWithHeadersAsync(
                    HttpMethod.Put,
                    $"api/v1/admin/redemptions/{redemptionId}/status",
                    payload,
                    systemId,
                    apiKey);

                if (putResponse.IsSuccessStatusCode)
                {
                    return true;
                }
            }

            var patchPayload = new object[]
            {
                new { status = "Fulfilled" },
                new { Status = "Fulfilled" }
            };

            foreach (var payload in patchPayload)
            {
                var patchResponse = await SendWithHeadersAsync(
                    HttpMethod.Patch,
                    $"api/v1/admin/redemptions/{redemptionId}/status",
                    payload,
                    systemId,
                    apiKey);

                if (patchResponse.IsSuccessStatusCode)
                {
                    return true;
                }
            }

            return false;
        }

        private async Task MarkRedemptionFulfilledAsync(Guid redemptionId, string? systemId, string? apiKey)
        {
            var isFulfilled = await TryFulfillRedemptionAsync(redemptionId, systemId, apiKey);
            if (isFulfilled)
            {
                return;
            }

            var fulfillResponse = await SendWithHeadersAsync(
                HttpMethod.Put,
                $"api/v1/admin/redemptions/{redemptionId}/status",
                new { status = "Fulfilled" },
                systemId,
                apiKey);

            if (!fulfillResponse.IsSuccessStatusCode)
            {
                var detail = await fulfillResponse.Content.ReadAsStringAsync();
                _logger.LogWarning("Auto-fulfill failed for redemption {RedemptionId}. HTTP {Status}. Body: {Body}", redemptionId, fulfillResponse.StatusCode, detail);
            }
        }

        private async Task AutoFulfillRedemptionAsync(Guid customerId, Guid rewardId, string? systemId, string? apiKey)
        {
            try
            {
                for (var attempt = 1; attempt <= 5; attempt++)
                {
                    var pendingResponse = await SendWithHeadersAsync(HttpMethod.Get, "api/v1/admin/redemptions/pending", null, systemId, apiKey);
                    if (!pendingResponse.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("Auto-fulfill attempt {Attempt}: could not fetch pending redemptions (HTTP {Status})", attempt, pendingResponse.StatusCode);
                        await Task.Delay(attempt * 250);
                        continue;
                    }

                    var root = await pendingResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();

                    // Support both a raw array [ {...} ] and wrapped objects
                    System.Text.Json.JsonElement pendingArray = default;
                    if (root.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        pendingArray = root;
                    }
                    else if (root.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        if (root.TryGetProperty("items", out var items) && items.ValueKind == System.Text.Json.JsonValueKind.Array)
                            pendingArray = items;
                        else if (root.TryGetProperty("data", out var data))
                        {
                            if (data.ValueKind == System.Text.Json.JsonValueKind.Array)
                                pendingArray = data;
                            else if (data.ValueKind == System.Text.Json.JsonValueKind.Object
                                     && data.TryGetProperty("items", out var nestedItems)
                                     && nestedItems.ValueKind == System.Text.Json.JsonValueKind.Array)
                                pendingArray = nestedItems;
                        }
                    }

                    if (pendingArray.ValueKind != System.Text.Json.JsonValueKind.Array)
                    {
                        _logger.LogWarning("Auto-fulfill attempt {Attempt}: unexpected pending redemptions response shape", attempt);
                        await Task.Delay(attempt * 250);
                        continue;
                    }

                    var candidates = pendingArray.EnumerateArray().Where(pending =>
                    {
                        if (!pending.TryGetProperty("externalUserId", out var userIdProp) ||
                            userIdProp.GetString() != customerId.ToString())
                            return false;

                        Guid pendingRewardId = Guid.Empty;
                        if (pending.TryGetProperty("rewardId", out var rewardIdProp))
                        {
                            if (rewardIdProp.ValueKind == System.Text.Json.JsonValueKind.String)
                                Guid.TryParse(rewardIdProp.GetString(), out pendingRewardId);
                            else
                                rewardIdProp.TryGetGuid(out pendingRewardId);
                        }

                        return pendingRewardId == rewardId;
                    }).ToList();

                    if (candidates.Count == 0)
                    {
                        await Task.Delay(attempt * 250);
                        continue;
                    }

                    foreach (var pending in candidates)
                    {
                        if (!pending.TryGetProperty("id", out var idProp) || !idProp.TryGetGuid(out var redemptionId))
                        {
                            _logger.LogWarning("Auto-fulfill: matched pending redemption has no valid 'id'");
                            continue;
                        }

                        await MarkRedemptionFulfilledAsync(redemptionId, systemId, apiKey);
                        _logger.LogInformation("Auto-fulfill: redemption {RedemptionId} fulfilled for customer {CustomerId}", redemptionId, customerId);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Auto-fulfill: unexpected error during auto-fulfillment for customer {CustomerId}, reward {RewardId}", customerId, rewardId);
            }
        }

        public async Task<Result<List<LoyaltyRuleDto>>> GetActiveRulesAsync(string? systemId = null, string? apiKey = null)
        {
            try
            {
                var response = await SendWithHeadersAsync(HttpMethod.Get, "api/v1/admin/rules", null, systemId, apiKey);
                if (response.IsSuccessStatusCode)
                {
                    var rules = await response.Content.ReadFromJsonAsync<List<LoyaltyRuleDto>>();
                    return rules != null ? Result<List<LoyaltyRuleDto>>.Success(rules) : Result<List<LoyaltyRuleDto>>.Failure("Empty response");
                }
                return Result<List<LoyaltyRuleDto>>.Failure($"Loyalty API error: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                return Result<List<LoyaltyRuleDto>>.Failure($"Failed to connect to Loyalty API: {ex.Message}");
            }
        }

        public async Task<Result<List<LoyaltyHistoryDto>>> GetCustomerHistoryAsync(Guid customerId, string? systemId = null, string? apiKey = null)
        {
            try
            {
                var accountResult = await GetCustomerLoyaltyAsync(customerId, systemId, apiKey);
                if (!accountResult.IsSuccess || accountResult.Value == null)
                    return Result<List<LoyaltyHistoryDto>>.Failure(accountResult.Error ?? "Account not found");

                var accountId = accountResult.Value.AccountId != Guid.Empty ? accountResult.Value.AccountId : accountResult.Value.Id;
                var response = await SendWithHeadersAsync(HttpMethod.Get, $"api/v1/accounts/{accountId}/history", null, systemId, apiKey);

                if (response.IsSuccessStatusCode)
                {
                    var history = await response.Content.ReadFromJsonAsync<List<LoyaltyHistoryDto>>();
                    return history != null ? Result<List<LoyaltyHistoryDto>>.Success(history) : Result<List<LoyaltyHistoryDto>>.Failure("Empty response");
                }
                return Result<List<LoyaltyHistoryDto>>.Failure($"Loyalty API error: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                return Result<List<LoyaltyHistoryDto>>.Failure($"Failed to connect to Loyalty API: {ex.Message}");
            }
        }

        public async Task<Result<LoyaltyAdminStatsResponse>> GetAdminStatsAsync(string? systemId = null, string? apiKey = null)
        {
            try
            {
                var targetSystemId = !string.IsNullOrWhiteSpace(systemId) ? systemId : _settings.SystemId;
                if (string.IsNullOrWhiteSpace(targetSystemId))
                {
                    return Result<LoyaltyAdminStatsResponse>.Failure("Loyalty system context is required.");
                }

                var response = await SendWithHeadersAsync(HttpMethod.Get, $"api/v1/admin/stats?SystemId={targetSystemId}", null, systemId, apiKey);
                if (response.IsSuccessStatusCode)
                {
                    var stats = await response.Content.ReadFromJsonAsync<LoyaltyAdminStatsResponse>();
                    return stats != null ? Result<LoyaltyAdminStatsResponse>.Success(stats) : Result<LoyaltyAdminStatsResponse>.Failure("Empty response");
                }
                return Result<LoyaltyAdminStatsResponse>.Failure($"Loyalty API error: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                return Result<LoyaltyAdminStatsResponse>.Failure($"Failed to connect to Loyalty API: {ex.Message}");
            }
        }

        public async Task<Result<PagedRedemptionHistoryResponse>> GetRedemptionHistoryAsync(int page = 1, int pageSize = 10, string? status = null, string? searchTerm = null, string? systemId = null, string? apiKey = null)
        {
            try
            {
                var targetSystemId = !string.IsNullOrWhiteSpace(systemId) ? systemId : _settings.SystemId;
                var url = $"api/v1/admin/redemptions/history?SystemId={targetSystemId}&Page={page}&PageSize={pageSize}";
                if (!string.IsNullOrEmpty(status)) url += $"&Status={status}";
                if (!string.IsNullOrEmpty(searchTerm)) url += $"&SearchTerm={searchTerm}";

                var response = await SendWithHeadersAsync(HttpMethod.Get, url, null, systemId, apiKey);
                if (response.IsSuccessStatusCode)
                {
                    var history = await response.Content.ReadFromJsonAsync<PagedRedemptionHistoryResponse>();
                    return history != null ? Result<PagedRedemptionHistoryResponse>.Success(history) : Result<PagedRedemptionHistoryResponse>.Failure("Empty response");
                }
                return Result<PagedRedemptionHistoryResponse>.Failure($"Loyalty API error: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                return Result<PagedRedemptionHistoryResponse>.Failure($"Failed to connect to Loyalty API: {ex.Message}");
            }
        }

        public async Task<Result<PagedLedgerHistoryResponse>> GetGlobalLedgerAsync(int page = 1, int pageSize = 10, string? searchTerm = null, string? systemId = null, string? apiKey = null)
        {
            try
            {
                var targetSystemId = !string.IsNullOrWhiteSpace(systemId) ? systemId : _settings.SystemId;
                var url = $"api/v1/admin/ledger/search-paged?SystemId={targetSystemId}&Page={page}&PageSize={pageSize}";
                if (!string.IsNullOrEmpty(searchTerm)) url += $"&SearchTerm={searchTerm}";

                var response = await SendWithHeadersAsync(HttpMethod.Get, url, null, systemId, apiKey);
                if (response.IsSuccessStatusCode)
                {
                    var history = await response.Content.ReadFromJsonAsync<PagedLedgerHistoryResponse>();
                    return history != null ? Result<PagedLedgerHistoryResponse>.Success(history) : Result<PagedLedgerHistoryResponse>.Failure("Empty response");
                }
                return Result<PagedLedgerHistoryResponse>.Failure($"Loyalty API error: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                return Result<PagedLedgerHistoryResponse>.Failure($"Failed to connect to Loyalty API: {ex.Message}");
            }
        }
    }
}
