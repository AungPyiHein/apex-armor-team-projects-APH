using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace MiniPos.Backend.Features.Loyalties;

public sealed class LoyaltyEngineOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string SystemId { get; set; } = string.Empty;
}

public sealed class LoyaltyEngineApiClient
{
    private readonly HttpClient _http;
    private readonly LoyaltyEngineOptions _options;

    public LoyaltyEngineApiClient(HttpClient http, IOptions<LoyaltyEngineOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public string SystemId => _options.SystemId;

    public async Task<(HttpStatusCode status, TResponse? data, string? body)> GetAsync<TResponse>(
        string relativePath,
        CancellationToken ct = default)
    {
        var res = await _http.GetAsync(relativePath, ct);
        var body = await res.Content.ReadAsStringAsync(ct);

        if (!res.IsSuccessStatusCode)
            return (res.StatusCode, default, body);

        var data = await res.Content.ReadFromJsonAsync<TResponse>(cancellationToken: ct);
        return (res.StatusCode, data, body);
    }

    public async Task<(HttpStatusCode status, TResponse? data, string? body)> PostAsync<TRequest, TResponse>(
        string relativePath,
        TRequest payload,
        CancellationToken ct = default)
    {
        var res = await _http.PostAsJsonAsync(relativePath, payload, ct);
        var body = await res.Content.ReadAsStringAsync(ct);

        if (!res.IsSuccessStatusCode)
            return (res.StatusCode, default, body);

        var data = await res.Content.ReadFromJsonAsync<TResponse>(cancellationToken: ct);
        return (res.StatusCode, data, body);
    }

    public async Task<(HttpStatusCode status, string? body)> PostAsync<TRequest>(
        string relativePath,
        TRequest payload,
        CancellationToken ct = default)
    {
        var res = await _http.PostAsJsonAsync(relativePath, payload, ct);
        var body = await res.Content.ReadAsStringAsync(ct);
        return (res.StatusCode, body);
    }
}

