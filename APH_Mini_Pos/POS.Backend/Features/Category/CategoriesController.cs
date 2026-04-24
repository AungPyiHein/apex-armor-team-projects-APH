using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using POS.Backend.Features.Category;
using POS.Backend.Common;

namespace POS.Backend.Features.Category
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryServices _categoryServices;
        public CategoriesController(ICategoryServices categoryServices)
        {
            _categoryServices = categoryServices;
        }
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllCategories([FromQuery] PaginationFilter filter)
        {
            var result = await _categoryServices.GetAllCategoriesAsync(filter);
            return result.IsSuccess ? Ok(new { IsSuccess = true, Message = "Categories retrieved successfully", Data = result.Value }) : BadRequest(new { IsSuccess = false, Message = result.Error });
        }
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCategoryById(Guid id)
        {
            var result = await _categoryServices.GetCategoryByIdAsync(id);
            return result.IsSuccess ? Ok(new { IsSuccess = true, Message = "Category retrieved successfully", Data = result.Value }) : NotFound(new { IsSuccess = false, Message = result.Error });
        }
        [HttpPost]
        [Authorize(Roles = "Admin,MerchantAdmin")]
        public async Task<IActionResult> CreateCategory(CreateCategoryRequest request)
        {
            var result = await _categoryServices.CreateCategoryAsync(request);
            return result.IsSuccess ? Ok(new { IsSuccess = true, Message = "Category Created", Data = result.Value }) : BadRequest(new { IsSuccess = false, Message = result.Error });
        }
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,MerchantAdmin")]
        public async Task<IActionResult> UpdateCategory(Guid id, UpdateCategoryRequest request)
        {
            if (id != request.Id)
                return BadRequest(new { IsSuccess = false, Message = "Invalid category ID." });
            var result = await _categoryServices.UpdateCategoryAsync(request);
            return result.IsSuccess ? Ok(new { IsSuccess = true, Message = "Category Updated" }) : BadRequest(new { IsSuccess = false, Message = result.Error });
        }
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,MerchantAdmin")]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            var result = await _categoryServices.DeleteCategoryAsync(id);
            return result.IsSuccess ? Ok(new { IsSuccess = true, Message = "Category Deleted" }) : BadRequest(new { IsSuccess = false, Message = result.Error });
        }
        [HttpPatch("{id}/restore")]
        public async Task<IActionResult> RestoreCategory(Guid id)
        {
            var result = await _categoryServices.RestoreCategoryAsync(id);
            return result.IsSuccess ? Ok(new { IsSuccess = true, Message = "Category Restored" }) : BadRequest(new { IsSuccess = false, Message = result.Error });
        }
        [HttpGet("deleted")]
        public async Task<IActionResult> GetDeletedCategories([FromQuery] PaginationFilter filter)
        {
            var result = await _categoryServices.GetDeletedCategoriesAsync(filter);
            return result.IsSuccess ? Ok(new { IsSuccess = true, Message = "Deleted categories retrieved successfully", Data = result.Value }) : BadRequest(new { IsSuccess = false, Message = result.Error });
        }
    }
}
