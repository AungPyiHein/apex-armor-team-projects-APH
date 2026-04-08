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
}
