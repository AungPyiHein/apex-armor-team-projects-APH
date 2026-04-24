using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Common;

namespace MiniPos.Backend.Features.Loyalties;

public interface ILoyaltyService
{
    Task<Result<CreateEventResponse>> CreateEventAsync(CreateEventRequest request);
    Task<Result<Account>> LookupAccountAsync(Guid customerId);
    Task<Result<List<Reward>>> GetActiveRewardsAsync();
    Task<Result> ClaimRewardAsync(ClaimRewardRequest request);
    Task<Result<List<PointHistory>>> GetPointHistoriesAsync(Guid accountId);
}

public class LoyaltyService : ILoyaltyService
{
    private readonly LoyaltyEngineApiClient _client;

    public LoyaltyService(LoyaltyEngineApiClient client)
    {
        _client = client;
    }

    public async Task<Result<CreateEventResponse>> CreateEventAsync(CreateEventRequest request)
    {
        const string errCode = "Royalty.CreateEvent";
        try
        {
            var (status, data, body) = await _client.PostAsync<CreateEventRequest, CreateEventResponse>(
                "/api/v1/events/process",
                request);

            if (status == HttpStatusCode.OK)
                return Result<CreateEventResponse>.Success(data);

            return Result<CreateEventResponse>.Failure(MapError(errCode, status, body));
        }
        catch (Exception e)
        {
            return Result<CreateEventResponse>.Failure(new InternalError(errCode, e.Message));
        }
    }

    public async Task<Result<Account>> LookupAccountAsync(Guid customerId)
    {
        const string errCode = "Royalty.LookupAccount";
        try
        {
            var path = $"/api/v1/accounts/lookup/{_client.SystemId}/{customerId}";
            var (status, data, body) = await _client.GetAsync<Account>(path);

            if (status == HttpStatusCode.OK && data != null)
                return Result<Account>.Success(data);

            return Result<Account>.Failure(MapError(errCode, status, body));
        }
        catch (Exception e)
        {
            return Result<Account>.Failure(new InternalError(errCode, e.Message));
        }
    }

    public async Task<Result<List<Reward>>> GetActiveRewardsAsync()
    {
        const string errCode = "Royalty.GetActiveRewards";
        try
        {
            var path = $"/api/v1/rewards/active/{_client.SystemId}";
            var (status, data, body) = await _client.GetAsync<List<Reward>>(path);

            if (status == HttpStatusCode.OK && data != null)
                return Result<List<Reward>>.Success(data);

            return Result<List<Reward>>.Failure(MapError(errCode, status, body));
        }
        catch (Exception e)
        {
            return Result<List<Reward>>.Failure(new InternalError(errCode, e.Message));
        }
    }

    public async Task<Result> ClaimRewardAsync(ClaimRewardRequest request)
    {
        const string errCode = "Royalty.ClaimReward";
        try
        {
            request.ExternalUserId = request.CustomerId.ToString();
            var (status, body) = await _client.PostAsync("/api/v1/redemption/claim", request);

            if ((int)status >= 200 && (int)status <= 299)
                return Result.Success();

            var errMsg = JsonSerializer.Deserialize<ErrorResponse>(body);
            return Result.Failure(new CustomError(status.ToString(), errMsg.Error));
        }
        catch (Exception e)
        {
            return Result.Failure(new InternalError(errCode, e.Message));
        }
    }

    public async Task<Result<List<PointHistory>>> GetPointHistoriesAsync(Guid accountId)
    {
        Console.WriteLine($"accountId: {accountId}");
        const string errCode = "Royalty.GetPointHistories";
        try
        {
            var path = $"/api/v1/accounts/{accountId}/history";
            var (status, data, body) = await _client.GetAsync<List<PointHistory>>(path);

            Console.WriteLine($"{data?.Count} and {status}");

            if (status == HttpStatusCode.OK && data != null)
                return Result<List<PointHistory>>.Success(data);

            return Result<List<PointHistory>>.Failure(MapError(errCode, status, body));
        }
        catch (Exception e)
        {
            return Result<List<PointHistory>>.Failure(new InternalError(errCode, e.Message));
        }
    }

    private static Error MapError(string errCode, HttpStatusCode status, string? body)
    {
        var msg = string.IsNullOrWhiteSpace(body) ? $"Loyalty API error ({(int)status})" : body;

        return status switch
        {
            HttpStatusCode.NotFound => new NotFoundError(errCode, msg),
            HttpStatusCode.Conflict => new ConflictError(errCode, msg),
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => new UnAuthorizedError(errCode, msg),
            HttpStatusCode.BadRequest => new ValidationError(errCode, msg),
            _ => new InternalError(errCode, msg)
        };
    }
}