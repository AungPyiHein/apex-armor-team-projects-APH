using Mapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniPos.Backend.Extensions;

namespace MiniPos.Backend.Features.Merchants;

[Authorize]
[ApiController]
[Route("api/merchants")]
public class MerchantController : ControllerBase
{
    private readonly IMerchantService _merchantService;

    public MerchantController(IMerchantService merchantService)
    {
        _merchantService = merchantService;
    }

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] MerchantListRequest filter)
    {
        filter.MerchantAdminId = User.GetUserId();
        var result = await _merchantService.GetList(filter);
        if (!result.IsSuccess)
        {
            var statusCode = ErrorHttpMapper.GetStatusCode(result.Error!);
            return StatusCode(statusCode, result.Error);
        }

        var pagedResult = result.Data!;
        if (pagedResult.TotalCount == 0) return NotFound(pagedResult);

        return Ok(result.Data);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var request = new MerchantGetByIdRequest
        {
            MerchantAdminId = User.GetUserId(),
            MerchantId = id
        };
        var result = await _merchantService.GetById(request);
        if (result.IsSuccess)
            return Ok(result.Data);

        var statusCode = ErrorHttpMapper.GetStatusCode(result.Error!);
        return StatusCode(statusCode, new { message = result.Error!.Message });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] MerchantCreateRequest request)
    {
        request.MerchantAdminId = User.GetUserId();
        var result = await _merchantService.Create(request);
        if (result.IsSuccess) return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data);

        var statusCode = ErrorHttpMapper.GetStatusCode(result.Error!);
        return StatusCode(statusCode, new { message = result.Error!.Message });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] MerchantUpdateRequest request)
    {
        request.MerchantAdminId = User.GetUserId();
        request.MerchantId = id;
        var result = await _merchantService.Update(request);
        if (result.IsSuccess)
            return Ok();

        var statusCode = ErrorHttpMapper.GetStatusCode(result.Error!);
        return StatusCode(statusCode, new { message = result.Error!.Message });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var request = new MerchantDeleteRequest
        {
            MerchantAdminId = User.GetUserId(),
            MerchantId = id
        };
        var result = await _merchantService.Delete(request);
        if (result.IsSuccess)
            return NoContent();

        var statusCode = ErrorHttpMapper.GetStatusCode(result.Error!);
        return StatusCode(statusCode, new { message = result.Error!.Message });
    }
}