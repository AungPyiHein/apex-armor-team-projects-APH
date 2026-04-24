using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Backend.Features.User;

namespace POS.Backend.Features.User
{
    [Authorize(Roles = "Admin,MerchantAdmin,Staff")]
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserServices _userServices;

        public UsersController(IUserServices userServices)
        {
            _userServices = userServices;
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            var result = await _userServices.CreateUserAsync(request);
            return result.IsSuccess ? Ok(new { IsSuccess = true, Message = "User Created", Data = result.Value }) : BadRequest(new { IsSuccess = false, Message = result.Error });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(Guid id)
        {
            var result = await _userServices.GetUserByIdAsync(id);
            return result.IsSuccess ? Ok(new { IsSuccess = true, Message = "User retrieved successfully", Data = result.Value }) : NotFound(new { IsSuccess = false, Message = result.Error });
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsers([FromQuery] PaginationFilter filter)
        {
            var result = await _userServices.GetAllUsersAsync(filter);
            return result.IsSuccess ? Ok(new { IsSuccess = true, Message = "Users retrieved successfully", Data = result.Value }) : BadRequest(new { IsSuccess = false, Message = result.Error });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
        {
            var result = await _userServices.UpdateUserAsync(id, request);
            return result.IsSuccess ? Ok(new { IsSuccess = true, Message = "User updated successfully." }) : BadRequest(new { IsSuccess = false, Message = result.Error });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var result = await _userServices.DeleteUserAsync(id);
            return result.IsSuccess ? Ok(new { IsSuccess = true, Message = "User deleted successfully." }) : BadRequest(new { IsSuccess = false, Message = result.Error });
        }
    }
}
