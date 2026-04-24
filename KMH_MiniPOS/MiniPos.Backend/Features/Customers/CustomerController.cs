using Mapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniPos.Backend.Extensions;

namespace MiniPos.Backend.Features.Customers;

[Authorize]
[ApiController]
[Route("api/customers")]
public class CustomerController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomerController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] CustomerListRequest filter)
    {
        filter.ProcessedById = User.GetUserId();
        var result = await _customerService.GetList(filter);
        if (result.IsSuccess)
            return Ok(result.Data);

        var statusCode = ErrorHttpMapper.GetStatusCode(result.Error!);
        return StatusCode(statusCode, result.Error?.Message);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _customerService.GetById(id);
        if (result.IsSuccess)
            return Ok(result.Data);

        var statusCode = ErrorHttpMapper.GetStatusCode(result.Error!);
        return StatusCode(statusCode, result.Error?.Message);
    }

    [HttpGet("lookup")]
    public async Task<IActionResult> Lookup([FromQuery] CustomerLookup request)
    {
        var result = await _customerService.Lookup(request);
        if (result.IsSuccess)
            return Ok(result.Data);

        var statusCode = ErrorHttpMapper.GetStatusCode(result.Error!);
        return StatusCode(statusCode, result.Error?.Message);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CustomerCreateRequest request)
    {
        var result = await _customerService.Create(request);
        if (result.IsSuccess)
            return Created();

        var statusCode = ErrorHttpMapper.GetStatusCode(result.Error!);
        return StatusCode(statusCode, result.Error?.Message);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CustomerUpdateRequest request)
    {
        var result = await _customerService.Update(id, request);
        if (result.IsSuccess)
            return Ok();

        var statusCode = ErrorHttpMapper.GetStatusCode(result.Error!);
        return StatusCode(statusCode, result.Error?.Message);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _customerService.Delete(id);
        if (result.IsSuccess)
            return NoContent();

        var statusCode = ErrorHttpMapper.GetStatusCode(result.Error!);
        return StatusCode(statusCode, result.Error?.Message);
    }
}