using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;
using POS.Frontend.Models;
using POS.Frontend.Models.Merchants;
using POS.Shared.Models;

namespace POS.Frontend.Services.Merchants;

public interface IBranchService
{
    Task<ApiResponse<PagedResponse<BranchResponseDto>>> GetAllBranchesAsync(PaginationFilter filter);
    Task<ApiResponse<PagedResponse<BranchResponseDto>>> GetBranchesByMerchantIdAsync(Guid merchantId, PaginationFilter filter);
    Task<ApiResponse<Guid>> CreateBranchAsync(CreateBranchRequest request);
    Task<ApiResponse> UpdateBranchAsync(Guid id, UpdateBranchRequest request);
    Task<ApiResponse> DeleteBranchAsync(Guid id);
}

public class BranchService : IBranchService
{
    private readonly HttpClient _http;

    public BranchService(HttpClient http)
    {
        _http = http;
    }

    public async Task<ApiResponse<PagedResponse<BranchResponseDto>>> GetAllBranchesAsync(PaginationFilter filter)
    {
        try
        {
            var url = $"/api/branch?pageNumber={filter.PageNumber}&pageSize={filter.PageSize}&searchTerm={Uri.EscapeDataString(filter.SearchTerm ?? "")}";
            var response = await _http.GetFromJsonAsync<ApiResponse<PagedResponse<BranchResponseDto>>>(url);
            if (response != null) response.IsSuccess = true;
            return response ?? new ApiResponse<PagedResponse<BranchResponseDto>> { IsSuccess = false, Message = "Empty response from server" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<PagedResponse<BranchResponseDto>> { IsSuccess = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<PagedResponse<BranchResponseDto>>> GetBranchesByMerchantIdAsync(Guid merchantId, PaginationFilter filter)
    {
        try
        {
            var url = $"/api/branch/merchant/{merchantId}?pageNumber={filter.PageNumber}&pageSize={filter.PageSize}&searchTerm={Uri.EscapeDataString(filter.SearchTerm ?? "")}";
            var response = await _http.GetFromJsonAsync<ApiResponse<PagedResponse<BranchResponseDto>>>(url);
            if (response != null) response.IsSuccess = true;
            return response ?? new ApiResponse<PagedResponse<BranchResponseDto>> { IsSuccess = false, Message = "Empty response from server" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<PagedResponse<BranchResponseDto>> { IsSuccess = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<Guid>> CreateBranchAsync(CreateBranchRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/branch", request);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
            if (result != null) result.IsSuccess = response.IsSuccessStatusCode;
            return result ?? new ApiResponse<Guid> { IsSuccess = false, Message = "Error creating branch" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<Guid> { IsSuccess = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse> UpdateBranchAsync(Guid id, UpdateBranchRequest request)
    {
        try
        {
            var response = await _http.PutAsJsonAsync($"/api/branch/{id}", request);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse>();
            if (result != null) result.IsSuccess = response.IsSuccessStatusCode;
            return result ?? new ApiResponse { IsSuccess = false, Message = "Error updating branch" };
        }
        catch (Exception ex)
        {
            return new ApiResponse { IsSuccess = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse> DeleteBranchAsync(Guid id)
    {
        try
        {
            var response = await _http.DeleteAsync($"/api/branch/{id}");
            var result = await response.Content.ReadFromJsonAsync<ApiResponse>();
            if (result != null) result.IsSuccess = response.IsSuccessStatusCode;
            return result ?? new ApiResponse { IsSuccess = false, Message = "Error deleting branch" };
        }
        catch (Exception ex)
        {
            return new ApiResponse { IsSuccess = false, Message = $"Error: {ex.Message}" };
        }
    }
}
