using System.Net.Http.Json;
using POS.Frontend.Models;
using POS.Frontend.Models.Merchants;
using POS.Shared.Models;

namespace POS.Frontend.Services.Merchants;

public interface IMerchantService
{
    Task<ApiResponse<PagedResponse<MerchantResponseDto>>> GetAllMerchantsAsync(PaginationFilter filter);
    Task<ApiResponse<MerchantResponseDto>> GetMerchantByIdAsync(Guid id);
    Task<ApiResponse<Guid>> CreateMerchantAsync(CreateMerchantRequest request);
    Task<ApiResponse> UpdateMerchantAsync(Guid id, UpdateMerchantRequest request);
    Task<ApiResponse> DeleteMerchantAsync(Guid id, bool force = false);
    Task<ApiResponse<PagedResponse<MerchantResponseDto>>> GetDeletedMerchantsAsync(PaginationFilter filter);
    Task<ApiResponse> RestoreMerchantAsync(Guid id, bool restoreAll = false);
}
public class MerchantService : IMerchantService
{
    private readonly HttpClient _http;

    public MerchantService(HttpClient http)
    {
        _http = http;
    }

    public async Task<ApiResponse<PagedResponse<MerchantResponseDto>>> GetAllMerchantsAsync(PaginationFilter filter)
    {
        try
        {
            var url = $"/api/merchants?pageNumber={filter.PageNumber}&pageSize={filter.PageSize}&searchTerm={Uri.EscapeDataString(filter.SearchTerm ?? "")}";
            var response = await _http.GetFromJsonAsync<ApiResponse<PagedResponse<MerchantResponseDto>>>(url);
            if (response != null) response.IsSuccess = true;
            return response ?? new ApiResponse<PagedResponse<MerchantResponseDto>> { IsSuccess = false, Message = "Error fetching merchants" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<PagedResponse<MerchantResponseDto>> { IsSuccess = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<MerchantResponseDto>> GetMerchantByIdAsync(Guid id)
    {
        try
        {
            var response = await _http.GetFromJsonAsync<ApiResponse<MerchantResponseDto>>($"/api/merchants/{id}");
            if (response != null) response.IsSuccess = true;
            return response ?? new ApiResponse<MerchantResponseDto> { IsSuccess = false, Message = "Merchant not found" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<MerchantResponseDto> { IsSuccess = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<Guid>> CreateMerchantAsync(CreateMerchantRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/merchants", request);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
            if (result != null) result.IsSuccess = response.IsSuccessStatusCode;
            return result ?? new ApiResponse<Guid> { IsSuccess = false, Message = "Error creating merchant" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<Guid> { IsSuccess = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse> UpdateMerchantAsync(Guid id, UpdateMerchantRequest request)
    {
        try
        {
            var response = await _http.PutAsJsonAsync($"/api/merchants/{id}", request);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse>();
            if (result != null) result.IsSuccess = response.IsSuccessStatusCode;
            return result ?? new ApiResponse { IsSuccess = false, Message = "Error updating merchant" };
        }
        catch (Exception ex)
        {
            return new ApiResponse { IsSuccess = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse> DeleteMerchantAsync(Guid id, bool force = false)
    {
        try
        {
            var response = await _http.DeleteAsync($"/api/merchants/{id}?force={force}");
            var result = await response.Content.ReadFromJsonAsync<ApiResponse>();
            if (result != null) result.IsSuccess = response.IsSuccessStatusCode;
            return result ?? new ApiResponse { IsSuccess = false, Message = "Error deleting merchant" };
        }
        catch (Exception ex)
        {
            return new ApiResponse { IsSuccess = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<PagedResponse<MerchantResponseDto>>> GetDeletedMerchantsAsync(PaginationFilter filter)
    {
        try
        {
            var url = $"/api/merchants/deleted?pageNumber={filter.PageNumber}&pageSize={filter.PageSize}&searchTerm={Uri.EscapeDataString(filter.SearchTerm ?? "")}";
            var response = await _http.GetFromJsonAsync<ApiResponse<PagedResponse<MerchantResponseDto>>>(url);
            if (response != null) response.IsSuccess = true;
            return response ?? new ApiResponse<PagedResponse<MerchantResponseDto>> { IsSuccess = false, Message = "Error fetching deleted merchants" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<PagedResponse<MerchantResponseDto>> { IsSuccess = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse> RestoreMerchantAsync(Guid id, bool restoreAll = false)
    {
        try
        {
            var response = await _http.PatchAsync($"/api/merchants/{id}/restore?restoreAll={restoreAll}", null);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse>();
            if (result != null) result.IsSuccess = response.IsSuccessStatusCode;
            return result ?? new ApiResponse { IsSuccess = false, Message = "Error restoring merchant" };
        }
        catch (Exception ex)
        {
            return new ApiResponse { IsSuccess = false, Message = $"Error: {ex.Message}" };
        }
    }
}
