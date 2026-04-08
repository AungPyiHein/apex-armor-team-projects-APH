using System.Net.Http.Json;
using POS.Frontend.Models;
using POS.Frontend.Models.Products;
using POS.Shared.Models;

namespace POS.Frontend.Services.Products;

public interface IProductService
{
    Task<ApiResponse<PagedResponse<ProductsResponseDto>>> GetProductsAsync(PaginationFilter filter);
    Task<ApiResponse<Guid>> CreateProductAsync(CreateProductRequest request);
    Task<ApiResponse<bool>> UpdateProductAsync(UpdateProductRequest request);
    Task<ApiResponse<bool>> DeleteProductAsync(Guid id);
}
public class ProductService : IProductService
{
    private readonly HttpClient _http;

    public ProductService(HttpClient http)
    {
        _http = http;
    }

    public async Task<ApiResponse<PagedResponse<ProductsResponseDto>>> GetProductsAsync(PaginationFilter filter)
    {
        var url = $"/api/products?pageNumber={filter.PageNumber}&pageSize={filter.PageSize}";
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            url += $"&searchTerm={Uri.EscapeDataString(filter.SearchTerm)}";
        }

        if (filter.CategoryId != null && filter.CategoryId != Guid.Empty)
        {
            url += $"&categoryId={filter.CategoryId}";
        }

        try
        {
            var response = await _http.GetFromJsonAsync<ApiResponse<PagedResponse<ProductsResponseDto>>>(url);
            if (response != null) response.IsSuccess = true;
            return response ?? new ApiResponse<PagedResponse<ProductsResponseDto>>
            {
                IsSuccess = false,
                Message = "Error connecting to server",
                Data = new PagedResponse<ProductsResponseDto>(new List<ProductsResponseDto>(), 0, 1, 10)
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<PagedResponse<ProductsResponseDto>>
            {
                IsSuccess = false,
                Message = $"Error: {ex.Message}",
                Data = new PagedResponse<ProductsResponseDto>(new List<ProductsResponseDto>(), 0, 1, 10)
            };
        }
    }
    public async Task<ApiResponse<Guid>> CreateProductAsync(CreateProductRequest request)
    {
        var response = await _http.PostAsJsonAsync("/api/products", request);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
        if (result != null) result.IsSuccess = response.IsSuccessStatusCode;
        return result ?? new ApiResponse<Guid> { IsSuccess = false, Message = "Error creating product" };
    }

    public async Task<ApiResponse<bool>> UpdateProductAsync(UpdateProductRequest request)
    {
        var response = await _http.PutAsJsonAsync($"/api/products/{request.Id}", request);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
        if (result != null) result.IsSuccess = response.IsSuccessStatusCode;
        return result ?? new ApiResponse<bool> { IsSuccess = false, Message = "Error updating product" };
    }

    public async Task<ApiResponse<bool>> DeleteProductAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"/api/products/{id}");
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
        if (result != null) result.IsSuccess = response.IsSuccessStatusCode;
        return result ?? new ApiResponse<bool> { IsSuccess = false, Message = "Error deleting product" };
    }
}
