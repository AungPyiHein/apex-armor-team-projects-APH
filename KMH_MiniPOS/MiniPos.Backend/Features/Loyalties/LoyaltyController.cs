using Mapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MiniPos.Backend.Features.Loyalties;

[Authorize]
[ApiController]
[Route("loyalties")]
public class LoyaltyController : ControllerBase
{
    private readonly ILoyaltyService _loyaltyService;

    public LoyaltyController(ILoyaltyService loyaltyService)
    {
        _loyaltyService = loyaltyService;
    }

    [HttpPost("events")]
    public async Task<IActionResult> ProcessEvent([FromBody] CreateEventRequest request)
    {
        var result = await _loyaltyService.CreateEventAsync(request);
        if (result.IsSuccess)
            return Ok(result.Data);

        var statusCode = ErrorHttpMapper.GetStatusCode(result.Error!);
        return StatusCode(statusCode, new { message = result.Error!.Message });
    }

    [HttpGet("customers/{customerId}")]
    public async Task<IActionResult> LookupAccount(Guid customerId)
    {
        var result = await _loyaltyService.LookupAccountAsync(customerId);
        if (result.IsSuccess)
            return Ok(result.Data);

        var statusCode = ErrorHttpMapper.GetStatusCode(result.Error!);
        return StatusCode(statusCode, new { message = result.Error!.Message });
    }

    [HttpGet("rewards")]
    public async Task<IActionResult> GetActiveRewards()
    {
        var result = await _loyaltyService.GetActiveRewardsAsync();
        if (result.IsSuccess)
            return Ok(result.Data);

        var statusCode = ErrorHttpMapper.GetStatusCode(result.Error!);
        return StatusCode(statusCode, new { message = result.Error!.Message });
    }

    [HttpPost("redemptions/claim")]
    public async Task<IActionResult> ClaimReward([FromBody] ClaimRewardRequest request)
    {
        var result = await _loyaltyService.ClaimRewardAsync(request);
        if (result.IsSuccess)
            return Ok();

        int.TryParse(result.Error.Code, out var status);
        return StatusCode(status, new { message = result.Error.Message });
    }

    [HttpGet("customers/{customerId:guid}/histories")]
    public async Task<IActionResult> GetHistory(Guid customerId)
    {
        var result = await _loyaltyService.GetPointHistoriesAsync(customerId);
        if (result.IsSuccess)
            return Ok(result.Data);

        var statusCode = ErrorHttpMapper.GetStatusCode(result.Error!);
        return StatusCode(statusCode, new { message = result.Error!.Message });
    }
}