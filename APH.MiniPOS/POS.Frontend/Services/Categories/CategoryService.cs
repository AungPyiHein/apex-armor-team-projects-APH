using System.Net.Http.Json;
using POS.Frontend.Models;
using POS.Frontend.Models.Categories;
using POS.Shared.Models;

namespace POS.Frontend.Services.Categories;
public interface ICategoryService
{
    Task<ApiResponse<PagedResponse<CategoryResponseDto>>> GetCategoriesAsync(PaginationFilter filter);
    Task<ApiResponse<CategoryResponseDto>> GetCategoryByIdAsync(Guid id);
    Task<ApiResponse<Guid>> CreateCategoryAsync(CreateCategoryRequest request);
    Task<ApiResponse<bool>> UpdateCategoryAsync(UpdateCategoryRequest request);
    Task<ApiResponse<bool>> DeleteCategoryAsync(Guid id);
    Task<ApiResponse<PagedResponse<CategoryResponseDto>>> GetDeletedCategoriesAsync(PaginationFilter filter);
    Task<ApiResponse<bool>> RestoreCategoryAsync(Guid id);
}

public class CategoryService : ICategoryService
{
    private readonly HttpClient _http;

    public CategoryService(HttpClient http)
    {
        _http = http;
    }

    public async Task<ApiResponse<PagedResponse<CategoryResponseDto>>> GetCategoriesAsync(PaginationFilter filter)
    {
        try
        {
            var url = $"/api/categories?pageNumber={filter.PageNumber}&pageSize={filter.PageSize}";
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                url += $"&searchTerm={Uri.EscapeDataString(filter.SearchTerm)}";
            }

            var response = await _http.GetFromJsonAsync<ApiResponse<PagedResponse<CategoryResponseDto>>>(url);
            if (response != null) response.IsSuccess = true;
            return response ?? new ApiResponse<PagedResponse<CategoryResponseDto>>
            {
                IsSuccess = false,
                Message = "Error connecting to server",
                Data = new PagedResponse<CategoryResponseDto>(new List<CategoryResponseDto>(), 0, 1, 10)
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<PagedResponse<CategoryResponseDto>>
            {
                IsSuccess = false,
                Message = $"Error: {ex.Message}",
                Data = new PagedResponse<CategoryResponseDto>(new List<CategoryResponseDto>(), 0, 1, 10)
            };
        }
    }

    public async Task<ApiResponse<CategoryResponseDto>> GetCategoryByIdAsync(Guid id)
    {
        try
        {
            var response = await _http.GetFromJsonAsync<ApiResponse<CategoryResponseDto>>($"/api/categories/{id}");
            if (response != null) response.IsSuccess = true;
            return response ?? new ApiResponse<CategoryResponseDto> { IsSuccess = false, Message = "Error connecting to server" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<CategoryResponseDto> { IsSuccess = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<Guid>> CreateCategoryAsync(CreateCategoryRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/categories", request);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
            if (result != null) result.IsSuccess = response.IsSuccessStatusCode;
            return result ?? new ApiResponse<Guid> { IsSuccess = false, Message = "Error connecting to server" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<Guid> { IsSuccess = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<bool>> UpdateCategoryAsync(UpdateCategoryRequest request)
    {
        try
        {
            var response = await _http.PutAsJsonAsync($"/api/categories/{request.Id}", request);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
            if (result != null) result.IsSuccess = response.IsSuccessStatusCode;
            return result ?? new ApiResponse<bool> { IsSuccess = false, Message = "Error connecting to server" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool> { IsSuccess = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<bool>> DeleteCategoryAsync(Guid id)
    {
        try
        {
            var response = await _http.DeleteAsync($"/api/categories/{id}");
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
            if (result != null) result.IsSuccess = response.IsSuccessStatusCode;
            return result ?? new ApiResponse<bool> { IsSuccess = false, Message = "Error connecting to server" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool> { IsSuccess = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<PagedResponse<CategoryResponseDto>>> GetDeletedCategoriesAsync(PaginationFilter filter)
    {
        try
        {
            var url = $"/api/categories/deleted?pageNumber={filter.PageNumber}&pageSize={filter.PageSize}";
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                url += $"&searchTerm={Uri.EscapeDataString(filter.SearchTerm)}";
            }

            var response = await _http.GetFromJsonAsync<ApiResponse<PagedResponse<CategoryResponseDto>>>(url);
            if (response != null) response.IsSuccess = true;
            return response ?? new ApiResponse<PagedResponse<CategoryResponseDto>>
            {
                IsSuccess = false,
                Message = "Error connecting to server",
                Data = new PagedResponse<CategoryResponseDto>(new List<CategoryResponseDto>(), 0, 1, 10)
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<PagedResponse<CategoryResponseDto>>
            {
                IsSuccess = false,
                Message = $"Error: {ex.Message}",
                Data = new PagedResponse<CategoryResponseDto>(new List<CategoryResponseDto>(), 0, 1, 10)
            };
        }
    }

    public async Task<ApiResponse<bool>> RestoreCategoryAsync(Guid id)
    {
        try
        {
            var response = await _http.PatchAsync($"/api/categories/{id}/restore", null);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
            if (result != null) result.IsSuccess = response.IsSuccessStatusCode;
            return result ?? new ApiResponse<bool> { IsSuccess = false, Message = "Error connecting to server" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool> { IsSuccess = false, Message = $"Error: {ex.Message}" };
        }
    }
}
