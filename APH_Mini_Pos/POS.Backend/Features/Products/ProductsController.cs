using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using POS.Backend.Features.Products;
using POS.Backend.Common;

namespace POS.Backend.Features.Products
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductsServices _productsServices;
        public ProductsController(IProductsServices productsServices)
        {
            _productsServices = productsServices;
        }
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllProducts([FromQuery] PaginationFilter filter)
        {
            var result = await _productsServices.GetAllProductsAsync(filter);
            return result.IsSuccess ? Ok(new { IsSuccess = true, Message = "Products retrieved successfully", Data = result.Value }) : BadRequest(new { IsSuccess = false, Message = result.Error });
        }
        [HttpPost]
        [Authorize(Roles = "Admin,MerchantAdmin")]
        public async Task<IActionResult> CreateProducts(CreateProductRequest request)
        {
            var result = await _productsServices.CreateProductAsync(request);
            return result.IsSuccess ? Ok(new { IsSuccess = true, Message = "Product Created", Data = result.Value }) : BadRequest(new { IsSuccess = false, Message = result.Error });
        }
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,MerchantAdmin")]
        public async Task<IActionResult> UpdateProducts(Guid id, UpdateProductRequest request)
        {
            if (id != request.Id)
            {
                return BadRequest(new { IsSuccess = false, Message = "ID mismatch" });
            }
            var result = await _productsServices.UpdateProductAsync(request);
            return result.IsSuccess ? Ok(new { IsSuccess = true, Message = "Product Updated" }) : BadRequest(new { IsSuccess = false, Message = result.Error });
        }
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProductById(Guid id)
        {
            var result = await _productsServices.GetProductById(id);
            return result.IsSuccess ? Ok(new { IsSuccess = true, Message = "Product retrieved successfully", Data = result.Value }) : NotFound(new { IsSuccess = false, Message = result.Error });
        }
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,MerchantAdmin")]
        public async Task<IActionResult> DeleteProduct(Guid id)
        {
            var result = await _productsServices.DeleteProductAsync(id);
            return result.IsSuccess ? Ok(new { IsSuccess = true, Message = "Product Deleted" }) : BadRequest(new { IsSuccess = false, Message = result.Error });
        }
    }
}
