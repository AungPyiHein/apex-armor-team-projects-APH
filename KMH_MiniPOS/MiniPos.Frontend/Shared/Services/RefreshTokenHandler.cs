using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using MiniPos.Frontend.Shared.Types;

namespace MiniPos.Frontend.Shared.Services;

using System.Net;
using Microsoft.AspNetCore.Components;

public class RefreshTokenHandler : DelegatingHandler
{
    private readonly NavigationManager _navManager;
    private readonly IHttpClientFactory _clientFactory;

    public RefreshTokenHandler(NavigationManager navManager, IHttpClientFactory clientFactory)
    {
        _navManager = navManager;
        _clientFactory = clientFactory;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return response;
        }

        var refreshClient = _clientFactory
            .CreateClient("AuthClient");
        refreshClient.BaseAddress = new Uri("http://localhost:5107/");

        var refreshRequest = new HttpRequestMessage(HttpMethod.Get, "api/auth/refresh");
        refreshRequest.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

        var refreshResponse = await refreshClient.SendAsync(refreshRequest, cancellationToken);
        if (refreshResponse.IsSuccessStatusCode)
        {
            var clonedRequest = CloneRequest(request);
            return await base.SendAsync(clonedRequest, cancellationToken);
        }
        else
        {
            var err = await refreshResponse.Content.ReadFromJsonAsync<ErrMessageResponse>(cancellationToken);
            _navManager.NavigateTo("/login");
            return response;
        }
    }

    private HttpRequestMessage CloneRequest(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Content = request.Content,
            Version = request.Version
        };
        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        clone.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

        return clone;
    }
}