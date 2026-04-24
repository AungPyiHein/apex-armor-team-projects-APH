using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Backend.Features.Inventory;

namespace POS.Backend.Features.Inventory
{
    [Authorize(Roles = "Admin,MerchantAdmin,Staff")]
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryServices _inventoryServices;

        public InventoryController(IInventoryServices inventoryServices)
        {
            _inventoryServices = inventoryServices;
        }

        [HttpGet("branch/{branchId}")]
        public async Task<IActionResult> GetBranchInventory(Guid branchId, [FromQuery] PaginationFilter filter)
        {
            var result = await _inventoryServices.GetBranchInventoryAsync(branchId, filter);
            return result.IsSuccess ? Ok(new { IsSuccess = true, Message = "Inventory retrieved successfully", Data = result.Value }) : BadRequest(new { IsSuccess = false, Message = result.Error });
        }

        [HttpGet("product/{productId}")]
        public async Task<IActionResult> GetProductInventory(Guid productId)
        {
            var result = await _inventoryServices.GetProductInventoryAsync(productId);
            return result.IsSuccess ? Ok(new { IsSuccess = true, Message = "Product inventory retrieved successfully", Data = result.Value }) : BadRequest(new { IsSuccess = false, Message = result.Error });
        }

        [HttpPost("adjust")]
        public async Task<IActionResult> AdjustStock([FromBody] UpdateStockRequest request)
        {
            var result = await _inventoryServices.AdjustStockAsync(request);
            return result.IsSuccess ? Ok(new { IsSuccess = true, Message = "Stock adjusted successfully", Data = result.Value }) : BadRequest(new { IsSuccess = false, Message = result.Error });
        }
    }
}
