using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace POS.Frontend.Services.Auth;

/// <summary>
/// Intercepts 401 responses and attempts a token refresh via the HttpOnly cookie.
/// The access_token cookie is sent automatically by the browser on every request.
/// </summary>
public class TokenInterceptor : DelegatingHandler
{
    private readonly IServiceProvider _serviceProvider;

    public TokenInterceptor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

        var absPath = request.RequestUri?.AbsolutePath;
        bool isAuthRequest = absPath?.Contains("/api/auth/") ?? false;

        var response = await base.SendAsync(request, cancellationToken);

        if (!isAuthRequest && response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            var authService = _serviceProvider.GetRequiredService<IAuthService>();
            var refreshed = await authService.RefreshTokenAsync();

            if (refreshed)
            {
                // Cookie is updated on the browser side — just retry the original request
                var requestClone = await CloneRequestAsync(request);
                return await base.SendAsync(requestClone, cancellationToken);
            }
        }

        return response;
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);
        clone.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

        if (request.Content != null)
        {
            var bytes = await request.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(bytes);

            if (request.Content.Headers != null)
            {
                foreach (var h in request.Content.Headers)
                    clone.Content.Headers.TryAddWithoutValidation(h.Key, h.Value);
            }
        }

        clone.Version = request.Version;

        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        foreach (var prop in request.Options)
            clone.Options.Set(new HttpRequestOptionsKey<object?>(prop.Key), prop.Value);

        return clone;
    }
}
