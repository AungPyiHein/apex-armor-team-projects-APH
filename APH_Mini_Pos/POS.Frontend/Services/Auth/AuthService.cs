using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Authorization;
using POS.Shared.Models;

namespace POS.Frontend.Services.Auth;

public interface IAuthService
{
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request);
    Task LogoutAsync();
    Task<bool> RefreshTokenAsync();
}

public class AuthService : IAuthService
{
    private readonly HttpClient _http;
    private readonly AuthenticationStateProvider _authStateProvider;

    public AuthService(HttpClient http, AuthenticationStateProvider authStateProvider)
    {
        _http = http;
        _authStateProvider = authStateProvider;
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request)
    {
        var response = await _http.PostAsJsonAsync("/api/auth/login", request);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (result != null)
            {
                ((CustomAuthStateProvider)_authStateProvider).NotifyUserAuthentication(result);
                return Result<AuthResponse>.Success(result);
            }
        }

        var error = await response.Content.ReadAsStringAsync();
        return Result<AuthResponse>.Failure(error ?? "Login failed.");
    }

    public async Task LogoutAsync()
    {
        try
        {
            await _http.PostAsync("/api/auth/revoke-token", null);
        }
        catch { /* Ignore network errors on logout */ }

        ((CustomAuthStateProvider)_authStateProvider).NotifyUserLogout();
    }

    private Task<bool>? _refreshTokenTask;

    public Task<bool> RefreshTokenAsync()
    {
        _refreshTokenTask ??= RefreshTokenInternalAsync();
        return _refreshTokenTask;
    }

    private async Task<bool> RefreshTokenInternalAsync()
    {
        try
        {
            var response = await _http.PostAsync("/api/auth/refresh-token", null);

            if (!response.IsSuccessStatusCode)
            {
                await LogoutAsync();
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (result == null)
            {
                await LogoutAsync();
                return false;
            }

            ((CustomAuthStateProvider)_authStateProvider).NotifyUserAuthentication(result);
            return true;
        }
        finally
        {
            _refreshTokenTask = null;
        }
    }
}
