using System.Net.Http.Json;
using POS.Frontend.Models;
using POS.Frontend.Models.Sales;
using POS.Shared.Models;

namespace POS.Frontend.Services.Sales;

public interface ISaleService
{
    Task<ApiResponse<PagedResponse<OrderResponseDto>>> GetAllOrdersAsync(PaginationFilter filter);
    Task<ApiResponse<OrderResponseDto>> GetOrderByIdAsync(Guid id);
    Task<ApiResponse<Guid>> CreateOrderAsync(CreateOrderRequest request);
}

public class SaleService : ISaleService
{
    private readonly HttpClient _http;

    public SaleService(HttpClient http)
    {
        _http = http;
    }

    public async Task<ApiResponse<PagedResponse<OrderResponseDto>>> GetAllOrdersAsync(PaginationFilter filter)
    {
        try
        {
            var url = $"/api/sales?pageNumber={filter.PageNumber}&pageSize={filter.PageSize}&searchTerm={Uri.EscapeDataString(filter.SearchTerm ?? "")}";
            if (filter.BranchId.HasValue)
            {
                url += $"&branchId={filter.BranchId.Value}";
            }
            if (filter.ProcessedById.HasValue)
            {
                url += $"&processedById={filter.ProcessedById.Value}";
            }
            if (filter.StartDate.HasValue)
            {
                url += $"&startDate={filter.StartDate.Value:yyyy-MM-dd}";
            }
            if (filter.EndDate.HasValue)
            {
                url += $"&endDate={filter.EndDate.Value:yyyy-MM-dd}";
            }
            var response = await _http.GetFromJsonAsync<ApiResponse<PagedResponse<OrderResponseDto>>>(url);
            if (response != null) response.IsSuccess = true;
            return response ?? new ApiResponse<PagedResponse<OrderResponseDto>> { IsSuccess = false, Message = "Error connecting to server" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<PagedResponse<OrderResponseDto>> { IsSuccess = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<OrderResponseDto>> GetOrderByIdAsync(Guid id)
    {
        try
        {
            var response = await _http.GetFromJsonAsync<ApiResponse<OrderResponseDto>>($"/api/sales/{id}");
            if (response != null) response.IsSuccess = true;
            return response ?? new ApiResponse<OrderResponseDto> { IsSuccess = false, Message = "Error connecting to server" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<OrderResponseDto> { IsSuccess = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<Guid>> CreateOrderAsync(CreateOrderRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/sales", request);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
            if (result != null) result.IsSuccess = response.IsSuccessStatusCode;
            return result ?? new ApiResponse<Guid> { IsSuccess = false, Message = "Error connecting to server" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<Guid> { IsSuccess = false, Message = $"Error: {ex.Message}" };
        }
    }
}
