using System.Net.Http.Json;
using POS.Frontend.Models;
using POS.Frontend.Models.Users;
using POS.Shared.Models;

namespace POS.Frontend.Services.Users;

public interface IUserService
{
    Task<ApiResponse<PagedResponse<UserResponseDto>>> GetAllUsersAsync(PaginationFilter filter);
    Task<ApiResponse<UserResponseDto>> GetUserByIdAsync(Guid id);
    Task<ApiResponse<Guid>> CreateUserAsync(CreateUserRequest request);
    Task<ApiResponse> UpdateUserAsync(Guid id, UpdateUserRequest request);
    Task<ApiResponse> DeleteUserAsync(Guid id);
}

public class UserService : IUserService
{
    private readonly HttpClient _http;

    public UserService(HttpClient http)
    {
        _http = http;
    }

    public async Task<ApiResponse<PagedResponse<UserResponseDto>>> GetAllUsersAsync(PaginationFilter filter)
    {
        try
        {
            var url = $"/api/users?pageNumber={filter.PageNumber}&pageSize={filter.PageSize}&searchTerm={Uri.EscapeDataString(filter.SearchTerm ?? "")}";
            var response = await _http.GetFromJsonAsync<ApiResponse<PagedResponse<UserResponseDto>>>(url);
            if (response != null) response.IsSuccess = true;
            return response ?? new ApiResponse<PagedResponse<UserResponseDto>> { IsSuccess = false, Message = "Error connecting to server" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<PagedResponse<UserResponseDto>> { IsSuccess = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<UserResponseDto>> GetUserByIdAsync(Guid id)
    {
        try
        {
            var response = await _http.GetFromJsonAsync<ApiResponse<UserResponseDto>>($"/api/users/{id}");
            if (response != null) response.IsSuccess = true;
            return response ?? new ApiResponse<UserResponseDto> { IsSuccess = false, Message = "Error connecting to server" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<UserResponseDto> { IsSuccess = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<Guid>> CreateUserAsync(CreateUserRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/users", request);
            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = await ExtractErrorMessageAsync(response);
                return new ApiResponse<Guid> { IsSuccess = false, Message = errorMsg };
            }
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
            if (result != null) result.IsSuccess = true;
            return result ?? new ApiResponse<Guid> { IsSuccess = false, Message = "Error connecting to server" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<Guid> { IsSuccess = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse> UpdateUserAsync(Guid id, UpdateUserRequest request)
    {
        try
        {
            var response = await _http.PutAsJsonAsync($"/api/users/{id}", request);
            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = await ExtractErrorMessageAsync(response);
                return new ApiResponse { IsSuccess = false, Message = errorMsg };
            }
            var result = await response.Content.ReadFromJsonAsync<ApiResponse>();
            if (result != null) result.IsSuccess = true;
            return result ?? new ApiResponse { IsSuccess = false, Message = "Error connecting to server" };
        }
        catch (Exception ex)
        {
            return new ApiResponse { IsSuccess = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<ApiResponse> DeleteUserAsync(Guid id)
    {
        try
        {
            var response = await _http.DeleteAsync($"/api/users/{id}");
            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = await ExtractErrorMessageAsync(response);
                return new ApiResponse { IsSuccess = false, Message = errorMsg };
            }
            var result = await response.Content.ReadFromJsonAsync<ApiResponse>();
            if (result != null) result.IsSuccess = true;
            return result ?? new ApiResponse { IsSuccess = false, Message = "Error connecting to server" };
        }
        catch (Exception ex)
        {
            return new ApiResponse { IsSuccess = false, Message = $"Error: {ex.Message}" };
        }
    }

    private async Task<string> ExtractErrorMessageAsync(HttpResponseMessage response)
    {
        var errorBody = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(errorBody)) return $"Server returned {response.StatusCode}";
        
        try
        {
            using var json = System.Text.Json.JsonDocument.Parse(errorBody);
            
            // Handle Validation Problem Details
            if (json.RootElement.TryGetProperty("errors", out var errorsProp) && errorsProp.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                var messages = new System.Collections.Generic.List<string>();
                foreach (var prop in errorsProp.EnumerateObject())
                {
                    if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        foreach (var err in prop.Value.EnumerateArray())
                        {
                            messages.Add(err.GetString() ?? "");
                        }
                    }
                    else
                    {
                        messages.Add(prop.Value.GetString() ?? "");
                    }
                }
                var joined = string.Join(" ", messages.Where(m => !string.IsNullOrWhiteSpace(m)));
                if (!string.IsNullOrWhiteSpace(joined)) return joined;
            }
            
            // Handle standard backend error formatting
            if (json.RootElement.TryGetProperty("message", out var msgProp) || json.RootElement.TryGetProperty("Message", out msgProp))
            {
                var msg = msgProp.GetString();
                if (!string.IsNullOrWhiteSpace(msg)) return msg;
            }
        }
        catch 
        {
            // Ignore parse errors, fallback to raw string
        }
        
        return errorBody.Length > 200 ? errorBody.Substring(0, 200) + "..." : errorBody;
    }
}
