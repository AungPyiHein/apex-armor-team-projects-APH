using System.Security.Claims;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Authorization;
using POS.Shared.Models;

namespace POS.Frontend.Services.Auth;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly HttpClient _http;
    private AuthenticationState _anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));
    private AuthenticationState _currentState;
    private bool _initialized = false;

    public CustomAuthStateProvider(HttpClient http)
    {
        _http = http;
        _currentState = _anonymous;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (!_initialized)
        {
            _initialized = true;
            await TryRestoreSessionAsync();
        }
        return _currentState;
    }

    private async Task TryRestoreSessionAsync()
    {
        try
        {
            var response = await _http.GetAsync("/api/auth/me");
            if (response.IsSuccessStatusCode)
            {
                var userInfo = await response.Content.ReadFromJsonAsync<AuthResponse>();
                if (userInfo != null)
                    SetAuthenticatedState(userInfo);
            }
        }
        catch
        {
            // No valid session — stay anonymous
        }
    }

    public void NotifyUserAuthentication(AuthResponse userInfo)
    {
        SetAuthenticatedState(userInfo);
        NotifyAuthenticationStateChanged(Task.FromResult(_currentState));
    }

    public void NotifyUserLogout()
    {
        _currentState = _anonymous;
        NotifyAuthenticationStateChanged(Task.FromResult(_currentState));
    }

    private void SetAuthenticatedState(AuthResponse userInfo)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, userInfo.Username),
            new Claim("sub", userInfo.Username),
            new Claim(ClaimTypes.Role, userInfo.Role),
        };

        if (userInfo.MerchantId.HasValue)
            claims.Add(new Claim("MerchantId", userInfo.MerchantId.Value.ToString()));

        if (userInfo.UserId.HasValue)
            claims.Add(new Claim("UserId", userInfo.UserId.Value.ToString()));

        if (userInfo.BranchId.HasValue)
            claims.Add(new Claim("BranchId", userInfo.BranchId.Value.ToString()));

        var identity = new ClaimsIdentity(claims, "jwt", "sub", ClaimTypes.Role);
        _currentState = new AuthenticationState(new ClaimsPrincipal(identity));
    }
}
