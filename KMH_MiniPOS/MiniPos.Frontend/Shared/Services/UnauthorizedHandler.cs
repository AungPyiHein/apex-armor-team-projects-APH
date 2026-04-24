namespace MiniPos.Frontend.Shared.Services;

using System.Net;
using Microsoft.AspNetCore.Components;

public class UnauthorizedHandler : DelegatingHandler
{
    private readonly NavigationManager _navManager;

    public UnauthorizedHandler(NavigationManager navManager)
    {
        _navManager = navManager;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _navManager.NavigateTo("/login");
        }

        return response;
    }
}