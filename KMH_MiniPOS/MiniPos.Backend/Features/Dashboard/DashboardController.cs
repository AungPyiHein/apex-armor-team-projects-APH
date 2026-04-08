using Mapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniPos.Backend.Extensions;

namespace MiniPos.Backend.Features.Dashboard;

[Authorize]
[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<IActionResult> GetStats([FromQuery] Guid merchantId, int days)
    {
        Console.WriteLine($"GetStats called with merchantId: {merchantId}, days: {days}");
        var request = new DashboardRequest
        {
            StatsDurationInDay = days,
            MerchantId = merchantId,
            ProcessedById = User.GetUserId()
        };
        var result = await _dashboardService.GetStats(request);
        if (result.IsSuccess)
            return Ok(result.Data);

        var statusCode = ErrorHttpMapper.GetStatusCode(result.Error!);
        return StatusCode(statusCode, new { message = result.Error?.Message });
    }
}