using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Backend.Features.Customers;
using POS.Backend.Common;

namespace POS.Backend.Features.Customers
{
    [Authorize(Roles = "Admin,MerchantAdmin,Staff")]
    [ApiController]
    [Route("api/[controller]")]
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerServices _customerServices;

        public CustomersController(ICustomerServices customerServices)
        {
            _customerServices = customerServices;
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomers([FromQuery] PaginationFilter filter)
        {
            var result = await _customerServices.GetCustomersAsync(Guid.Empty, filter);
            return result.IsSuccess ? Ok(new { IsSuccess = true, Message = "Customers retrieved successfully", Data = result.Value }) : BadRequest(new { IsSuccess = false, Message = result.Error });
        }

        [HttpGet("merchant/{merchantId}")]
        public async Task<IActionResult> GetCustomers(Guid merchantId, [FromQuery] PaginationFilter filter)
        {
            var result = await _customerServices.GetCustomersAsync(merchantId, filter);
            return result.IsSuccess ? Ok(new { IsSuccess = true, Message = "Customers retrieved successfully", Data = result.Value }) : BadRequest(new { IsSuccess = false, Message = result.Error });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCustomer(Guid id)
        {
            var result = await _customerServices.GetCustomerByIdAsync(id);
            return result.IsSuccess ? Ok(new { IsSuccess = true, Message = "Customer retrieved successfully", Data = result.Value }) : NotFound(new { IsSuccess = false, Message = result.Error });
        }

        [HttpPost]
        public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerRequest request)
        {
            var result = await _customerServices.CreateCustomerAsync(request);
            return result.IsSuccess ? CreatedAtAction(nameof(GetCustomer), new { id = result.Value }, new { IsSuccess = true, Message = "Customer Created", Data = result.Value }) : BadRequest(new { IsSuccess = false, Message = result.Error });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCustomer(Guid id, [FromBody] CreateCustomerRequest request)
        {
            var result = await _customerServices.UpdateCustomerAsync(id, request);
            return result.IsSuccess ? Ok(new { IsSuccess = true, Message = "Customer Updated" }) : BadRequest(new { IsSuccess = false, Message = result.Error });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(Guid id)
        {
            var result = await _customerServices.DeleteCustomerAsync(id);
            return result.IsSuccess ? Ok(new { IsSuccess = true, Message = "Customer Deleted" }) : BadRequest(new { IsSuccess = false, Message = result.Error });
        }
    }
}
