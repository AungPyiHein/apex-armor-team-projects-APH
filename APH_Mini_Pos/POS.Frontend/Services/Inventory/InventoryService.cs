using System.Net.Http.Json;
using POS.Frontend.Models;
using POS.Frontend.Models.Inventory;
using POS.Shared.Models;

namespace POS.Frontend.Services.Inventory;

public interface IInventoryService
{
    Task<ApiResponse<PagedResponse<InventoryResponseDto>>> GetBranchInventoryAsync(Guid branchId, PaginationFilter filter);
    Task<ApiResponse<IEnumerable<InventoryResponseDto>>> GetProductInventoryAsync(Guid productId);
    Task<ApiResponse<bool>> AdjustStockAsync(UpdateStockRequest request);
}
public class InventoryService : IInventoryService
{
    private readonly HttpClient _http;

    public InventoryService(HttpClient http)
    {
        _http = http;
    }

    public async Task<ApiResponse<PagedResponse<InventoryResponseDto>>> GetBranchInventoryAsync(Guid branchId, PaginationFilter filter)
    {
        try
        {
            var url = $"/api/inventory/branch/{branchId}?pageNumber={filter.PageNumber}&pageSize={filter.PageSize}&searchTerm={Uri.EscapeDataString(filter.SearchTerm ?? "")}";
            var response = await _http.GetFromJsonAsync<ApiResponse<PagedResponse<InventoryResponseDto>>>(url);
            if (response != null) response.IsSuccess = true;
            return response ?? new ApiResponse<PagedResponse<InventoryResponseDto>> { IsSuccess = false, Message = "Empty response from server" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<PagedResponse<InventoryResponseDto>> { IsSuccess = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<IEnumerable<InventoryResponseDto>>> GetProductInventoryAsync(Guid productId)
    {
        try
        {
            var url = $"/api/inventory/product/{productId}";
            var response = await _http.GetFromJsonAsync<ApiResponse<IEnumerable<InventoryResponseDto>>>(url);
            if (response != null) response.IsSuccess = response.Data != null;
            return response ?? new ApiResponse<IEnumerable<InventoryResponseDto>> { IsSuccess = false, Message = "Empty response from server" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<IEnumerable<InventoryResponseDto>> { IsSuccess = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<bool>> AdjustStockAsync(UpdateStockRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/inventory/adjust", request);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
            if (result != null) result.IsSuccess = response.IsSuccessStatusCode;
            return result ?? new ApiResponse<bool> { IsSuccess = false, Message = "Error adjusting stock" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool> { IsSuccess = false, Message = $"Error: {ex.Message}" };
        }
    }
}
