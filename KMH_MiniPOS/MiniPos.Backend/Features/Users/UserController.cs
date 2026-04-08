using Mapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniPos.Backend.Extensions;

namespace MiniPos.Backend.Features.Users;

[Authorize]
[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] UserListRequest filter)
    {
        var userId = User.GetUserId();
        filter.ProcessedById = userId;

        var result = await _userService.GetList(filter);
        if (!result.IsSuccess)
        {
            var statusCode = ErrorHttpMapper.GetStatusCode(result.Error!);
            return StatusCode(statusCode, result.Error);
        }

        var pagedResult = result.Data!;
        if (pagedResult.TotalCount == 0) return NotFound(pagedResult);

        return Ok(result.Data);
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetById(Guid userId)
    {
        var processedById = User.GetUserId();
        var request = new UserGetByIdRequest
        {
            UserId = userId,
            ProcessedById = processedById,
        };
        var result = await _userService.GetById(request);
        if (result.IsSuccess)
            return Ok(result.Data);

        var statusCode = ErrorHttpMapper.GetStatusCode(result.Error!);
        return StatusCode(statusCode, new { message = result.Error!.Message });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UserCreateRequest request)
    {
        request.ProcessedById = User.GetUserId();
        var result = await _userService.Create(request);
        if (result.IsSuccess) return Created();

        var statusCode = ErrorHttpMapper.GetStatusCode(result.Error!);
        return StatusCode(statusCode, new { message = result.Error!.Message });
    }

    [HttpPut("{userId}")]
    public async Task<IActionResult> Update(Guid userId, [FromBody] UserUpdateRequest request)
    {
        request.ProcessedById = User.GetUserId();
        request.UserId = userId;

        var result = await _userService.Update(request);
        if (result.IsSuccess)
            return Ok();

        var statusCode = ErrorHttpMapper.GetStatusCode(result.Error!);
        return StatusCode(statusCode, new { message = result.Error!.Message });
    }

    [HttpPost("{userId}/reset-password")]
    public async Task<IActionResult> UpdatePassword(Guid userId, [FromBody] UserResetPasswordRequest request)
    {
        request.ProcessedById = User.GetUserId();
        request.UserId = userId;

        var result = await _userService.ResetPassword(request);
        if (result.IsSuccess)
            return Ok();

        var statusCode = ErrorHttpMapper.GetStatusCode(result.Error!);
        return StatusCode(statusCode, new { message = result.Error!.Message });
    }

    [HttpPost("{userId}/reassign-branch")]
    public async Task<IActionResult> ReassignBranch(Guid userId, [FromBody] UserAssignBranchRequest request)
    {
        request.ProcessedById = User.GetUserId();
        request.UserId = userId;

        var result = await _userService.AssignBranch(request);
        if (result.IsSuccess)
            return Ok();

        var statusCode = ErrorHttpMapper.GetStatusCode(result.Error!);
        return StatusCode(statusCode, new { message = result.Error!.Message });
    }

    [HttpDelete("{userId}")]
    public async Task<IActionResult> Delete(Guid userId)
    {
        var request = new UserDeactivateRequest
        {
            UserId = userId,
            ProcessedById = User.GetUserId(),
        };

        var result = await _userService.Deactivate(request);
        if (result.IsSuccess)
            return NoContent();

        var statusCode = ErrorHttpMapper.GetStatusCode(result.Error!);
        return StatusCode(statusCode, new { message = result.Error!.Message });
    }
}