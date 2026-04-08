using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Backend.Features.Sales;

namespace POS.Backend.Features.Sales
{
    [ApiController]
    [Route("api/[controller]")]
    public class SalesController : ControllerBase
    {
        private readonly ISalesServices _salesServices;

        public SalesController(ISalesServices salesServices)
        {
            _salesServices = salesServices;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            var result = await _salesServices.CreateOrderAsync(request);
            return result.IsSuccess ? CreatedAtAction(nameof(GetOrder), new { id = result.Value }, new { IsSuccess = true, Message = "Order Created", Data = result.Value }) : BadRequest(new { IsSuccess = false, Message = result.Error });
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllOrders([FromQuery] PaginationFilter filter)
        {
            var result = await _salesServices.GetAllOrdersAsync(filter);
            return result.IsSuccess ? Ok(new { IsSuccess = true, Message = "Orders retrieved successfully", Data = result.Value }) : BadRequest(new { IsSuccess = false, Message = result.Error });
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetOrder(Guid id)
        {
            var result = await _salesServices.GetOrderByIdAsync(id);
            return result.IsSuccess ? Ok(new { IsSuccess = true, Message = "Order retrieved successfully", Data = result.Value }) : NotFound(new { IsSuccess = false, Message = result.Error });
        }
    }
}
