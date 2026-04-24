using System.Net.Http.Json;
using POS.Frontend.Models;
using POS.Frontend.Models.Sales;
using POS.Shared.Models;

namespace POS.Frontend.Services.Sales;

public interface ICustomerService
{
    Task<ApiResponse<PagedResponse<CustomerResponseDto>>> GetCustomersAsync(Guid? merchantId, PaginationFilter filter);
    Task<ApiResponse<CustomerResponseDto>> GetCustomerByIdAsync(Guid id);
    Task<ApiResponse<Guid>> CreateCustomerAsync(CreateCustomerRequest request);
    Task<ApiResponse> UpdateCustomerAsync(Guid id, CreateCustomerRequest request);
    Task<ApiResponse> DeleteCustomerAsync(Guid id);
    Task<ApiResponse<LoyaltyAccountResponse>> GetCustomerLoyaltyAsync(Guid customerId);
    Task<ApiResponse<List<LoyaltyReward>>> GetActiveRewardsAsync(Guid? customerId = null, Guid? merchantId = null);
    Task<ApiResponse<bool>> ClaimRewardAsync(ClaimRewardRequest request);
    Task<ApiResponse<List<LoyaltyRuleDto>>> GetLoyaltyRulesAsync();
    Task<ApiResponse<List<LoyaltyHistoryDto>>> GetCustomerHistoryAsync(Guid customerId);
    Task<ApiResponse<LoyaltyAdminStatsResponse>> GetAdminStatsAsync(Guid? merchantId = null);
    Task<ApiResponse<PagedRedemptionHistoryResponse>> GetRedemptionHistoryAsync(Guid? merchantId = null, int page = 1, int pageSize = 10, string? status = null, string? searchTerm = null);
    Task<ApiResponse<PagedLedgerHistoryResponse>> GetGlobalLedgerAsync(Guid? merchantId = null, int page = 1, int pageSize = 10, string? searchTerm = null);
    Task<ApiResponse<bool>> FulfillRedemptionAsync(Guid redemptionId, Guid? merchantId = null);
}
public class CustomerService : ICustomerService
{
    private readonly HttpClient _http;

    public CustomerService(HttpClient http)
    {
        _http = http;
    }

    public async Task<ApiResponse<PagedResponse<CustomerResponseDto>>> GetCustomersAsync(Guid? merchantId, PaginationFilter filter)
    {
        try
        {
            var url = (merchantId == null || merchantId == Guid.Empty) 
                ? $"/api/customers?pageNumber={filter.PageNumber}&pageSize={filter.PageSize}"
                : $"/api/customers/merchant/{merchantId}?pageNumber={filter.PageNumber}&pageSize={filter.PageSize}";
            
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                url += $"&searchTerm={Uri.EscapeDataString(filter.SearchTerm)}";
            }


            var response = await _http.GetFromJsonAsync<ApiResponse<PagedResponse<CustomerResponseDto>>>(url);
            if (response != null) response.IsSuccess = true;
            return response ?? new ApiResponse<PagedResponse<CustomerResponseDto>> { IsSuccess = false, Message = "Error connecting to server" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<PagedResponse<CustomerResponseDto>> { IsSuccess = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<CustomerResponseDto>> GetCustomerByIdAsync(Guid id)
    {
        try
        {
            var response = await _http.GetFromJsonAsync<ApiResponse<CustomerResponseDto>>($"/api/customers/{id}");
            if (response != null) response.IsSuccess = true;
            return response ?? new ApiResponse<CustomerResponseDto> { IsSuccess = false, Message = "Error connecting to server" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<CustomerResponseDto> { IsSuccess = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<Guid>> CreateCustomerAsync(CreateCustomerRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/customers", request);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
            if (result != null) result.IsSuccess = response.IsSuccessStatusCode;
            return result ?? new ApiResponse<Guid> { IsSuccess = false, Message = "Error creating customer" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<Guid> { IsSuccess = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse> UpdateCustomerAsync(Guid id, CreateCustomerRequest request)
    {
        try
        {
            var response = await _http.PutAsJsonAsync($"/api/customers/{id}", request);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse>();
            if (result != null) result.IsSuccess = response.IsSuccessStatusCode;
            return result ?? new ApiResponse { IsSuccess = false, Message = "Error updating customer" };
        }
        catch (Exception ex)
        {
            return new ApiResponse { IsSuccess = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse> DeleteCustomerAsync(Guid id)
    {
        try
        {
            var response = await _http.DeleteAsync($"/api/customers/{id}");
            var result = await response.Content.ReadFromJsonAsync<ApiResponse>();
            if (result != null) result.IsSuccess = response.IsSuccessStatusCode;
            return result ?? new ApiResponse { IsSuccess = false, Message = "Error deleting customer" };
        }
        catch (Exception ex)
        {
            return new ApiResponse { IsSuccess = false, Message = $"Error: {ex.Message}" };
        }
    }
    public async Task<ApiResponse<LoyaltyAccountResponse>> GetCustomerLoyaltyAsync(Guid customerId)
    {
        try
        {
            var response = await _http.GetFromJsonAsync<Result<LoyaltyAccountResponse>>($"/api/v1/loyalty/customer/{customerId}");
            if (response != null && response.IsSuccess)
            {
                return new ApiResponse<LoyaltyAccountResponse> { IsSuccess = true, Data = response.Value };
            }
            return new ApiResponse<LoyaltyAccountResponse> { IsSuccess = false, Message = response?.Error ?? "Loyalty data not found" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<LoyaltyAccountResponse> { IsSuccess = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<List<LoyaltyReward>>> GetActiveRewardsAsync(Guid? customerId = null, Guid? merchantId = null)
    {
        try
        {
            var queryParts = new List<string>();
            if (customerId.HasValue && customerId.Value != Guid.Empty)
            {
                queryParts.Add($"customerId={customerId.Value}");
            }

            if (merchantId.HasValue && merchantId.Value != Guid.Empty)
            {
                queryParts.Add($"merchantId={merchantId.Value}");
            }

            var url = "/api/v1/loyalty/rewards";
            if (queryParts.Count > 0)
            {
                url += "?" + string.Join("&", queryParts);
            }

            var response = await _http.GetFromJsonAsync<Result<List<LoyaltyReward>>>(url);
            if (response != null && response.IsSuccess)
            {
                return new ApiResponse<List<LoyaltyReward>> { IsSuccess = true, Data = response.Value };
            }
            return new ApiResponse<List<LoyaltyReward>> { IsSuccess = false, Message = response?.Error ?? "Rewards not found" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<LoyaltyReward>> { IsSuccess = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<bool>> ClaimRewardAsync(ClaimRewardRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync($"/api/v1/loyalty/claim", request);
            var result = await response.Content.ReadFromJsonAsync<Result<bool>>();
            if (result != null && result.IsSuccess)
            {
                return new ApiResponse<bool> { IsSuccess = true, Data = true };
            }
            return new ApiResponse<bool> { IsSuccess = false, Message = result?.Error ?? "Claim failed" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool> { IsSuccess = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<List<LoyaltyRuleDto>>> GetLoyaltyRulesAsync()
    {
        try
        {
            var response = await _http.GetFromJsonAsync<Result<List<LoyaltyRuleDto>>>($"/api/v1/loyalty/rules");
            if (response != null && response.IsSuccess)
            {
                return new ApiResponse<List<LoyaltyRuleDto>> { IsSuccess = true, Data = response.Value };
            }
            return new ApiResponse<List<LoyaltyRuleDto>> { IsSuccess = false, Message = response?.Error ?? "Rules not found" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<LoyaltyRuleDto>> { IsSuccess = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<List<LoyaltyHistoryDto>>> GetCustomerHistoryAsync(Guid customerId)
    {
        try
        {
            var response = await _http.GetFromJsonAsync<Result<List<LoyaltyHistoryDto>>>($"/api/v1/loyalty/customer/{customerId}/history");
            if (response != null && response.IsSuccess)
            {
                return new ApiResponse<List<LoyaltyHistoryDto>> { IsSuccess = true, Data = response.Value };
            }
            return new ApiResponse<List<LoyaltyHistoryDto>> { IsSuccess = false, Message = response?.Error ?? "History not found" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<LoyaltyHistoryDto>> { IsSuccess = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<LoyaltyAdminStatsResponse>> GetAdminStatsAsync(Guid? merchantId = null)
    {
        try
        {
            var url = "/api/v1/loyalty/admin/stats";
            if (merchantId.HasValue && merchantId.Value != Guid.Empty)
            {
                url += $"?merchantId={merchantId.Value}";
            }

            var response = await _http.GetFromJsonAsync<Result<LoyaltyAdminStatsResponse>>(url);
            if (response != null && response.IsSuccess)
            {
                return new ApiResponse<LoyaltyAdminStatsResponse> { IsSuccess = true, Data = response.Value };
            }
            return new ApiResponse<LoyaltyAdminStatsResponse> { IsSuccess = false, Message = response?.Error ?? "Stats not found" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<LoyaltyAdminStatsResponse> { IsSuccess = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<PagedRedemptionHistoryResponse>> GetRedemptionHistoryAsync(Guid? merchantId = null, int page = 1, int pageSize = 10, string? status = null, string? searchTerm = null)
    {
        try
        {
            var url = $"/api/v1/loyalty/admin/redemptions/history?page={page}&pageSize={pageSize}";
            if (merchantId.HasValue && merchantId.Value != Guid.Empty) url += $"&merchantId={merchantId.Value}";
            if (!string.IsNullOrEmpty(status)) url += $"&status={status}";
            if (!string.IsNullOrEmpty(searchTerm)) url += $"&searchTerm={searchTerm}";

            var response = await _http.GetFromJsonAsync<Result<PagedRedemptionHistoryResponse>>(url);
            if (response != null && response.IsSuccess)
            {
                return new ApiResponse<PagedRedemptionHistoryResponse> { IsSuccess = true, Data = response.Value };
            }
            return new ApiResponse<PagedRedemptionHistoryResponse> { IsSuccess = false, Message = response?.Error ?? "History not found" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<PagedRedemptionHistoryResponse> { IsSuccess = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<PagedLedgerHistoryResponse>> GetGlobalLedgerAsync(Guid? merchantId = null, int page = 1, int pageSize = 10, string? searchTerm = null)
    {
        try
        {
            var url = $"/api/v1/loyalty/admin/global-ledger?page={page}&pageSize={pageSize}";
            if (merchantId.HasValue && merchantId.Value != Guid.Empty) url += $"&merchantId={merchantId.Value}";
            if (!string.IsNullOrEmpty(searchTerm)) url += $"&searchTerm={searchTerm}";

            var response = await _http.GetFromJsonAsync<Result<PagedLedgerHistoryResponse>>(url);
            if (response != null && response.IsSuccess)
            {
                return new ApiResponse<PagedLedgerHistoryResponse> { IsSuccess = true, Data = response.Value };
            }
            return new ApiResponse<PagedLedgerHistoryResponse> { IsSuccess = false, Message = response?.Error ?? "Ledger not found" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<PagedLedgerHistoryResponse> { IsSuccess = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<bool>> FulfillRedemptionAsync(Guid redemptionId, Guid? merchantId = null)
    {
        try
        {
            var url = $"/api/v1/loyalty/admin/redemptions/{redemptionId}/fulfill";
            if (merchantId.HasValue && merchantId.Value != Guid.Empty)
            {
                url += $"?merchantId={merchantId.Value}";
            }

            var response = await _http.PostAsync(url, null);
            var result = await response.Content.ReadFromJsonAsync<Result<bool>>();
            if (result != null && result.IsSuccess)
            {
                return new ApiResponse<bool> { IsSuccess = true, Data = true };
            }

            return new ApiResponse<bool> { IsSuccess = false, Message = result?.Error ?? "Failed to fulfill redemption" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool> { IsSuccess = false, Message = $"Error: {ex.Message}" };
        }
    }
}
