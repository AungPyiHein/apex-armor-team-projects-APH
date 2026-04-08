using Mapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniPos.Backend.Extensions;

namespace MiniPos.Backend.Features.Products;

[Authorize]
[ApiController]
[Route("api/products")]
public class ProductController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] ProductListRequest filter)
    {
        filter.ProcessedById = User.GetUserId();
        var result = await _productService.GetList(filter);
        if (!result.IsSuccess)
        {
            var statusCode = ErrorHttpMapper.GetStatusCode(result.Error!);
            return StatusCode(statusCode, result.Error);
        }

        var pagedResult = result.Data!;
        if (pagedResult.TotalCount == 0)
        {
            return NotFound(pagedResult);
        }

        return Ok(result.Data);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _productService.GetById(id);
        if (result.IsSuccess)
            return Ok(result.Data);
        
        var statusCode = ErrorHttpMapper.GetStatusCode(result.Error!);
        return StatusCode(statusCode, new { message = result.Error!.Message });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProductCreateRequest request)
    {
        var result = await _productService.Create(request);
        if (result.IsSuccess)
        {
            return Created();
        }

        var statusCode = ErrorHttpMapper.GetStatusCode(result.Error!);
        return StatusCode(statusCode, new { message = result.Error!.Message });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ProductUpdateRequest request)
    {
        var result = await _productService.Update(id, request);
        if (result.IsSuccess)
            return Ok();

        var statusCode = ErrorHttpMapper.GetStatusCode(result.Error!);
        return StatusCode(statusCode, new { message = result.Error!.Message });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _productService.Delete(id);
        if (result.IsSuccess)
            return NoContent();

        var statusCode = ErrorHttpMapper.GetStatusCode(result.Error!);
        return StatusCode(statusCode, new { message = result.Error!.Message });
    }
}