using Common;
using Mapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using MiniPos.Backend.Extensions;
using MiniPos.Backend.Features.Users;

namespace MiniPos.Backend.Features.Profile;

[Authorize]
[ApiController]
[Route("api/profile")]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;
    private readonly IUserService _userService;
    
    public ProfileController(IProfileService profileService, IUserService userService)
    {
        _profileService = profileService;
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.GetUserId();
        var result = await _profileService.GetProfile(userId);
        
        if (result.IsSuccess)
            return Ok(result.Data);

        var statusCode = ErrorHttpMapper.GetStatusCode(result.Error!);
        return StatusCode(statusCode, new { message = result.Error?.Message });
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var req = new UserUpdateRequest
        {
            Email = request.Email,
            UserName = request.Name,
            ProcessedById = User.GetUserId(),
            UserId = User.GetUserId(),
        };
        
        var result = await _userService.Update(req);
        
        if (result.IsSuccess)
            return Ok();

        var statusCode = ErrorHttpMapper.GetStatusCode(result.Error!);
        return StatusCode(statusCode, new { message = result.Error?.Message });
    }

    [HttpPut("password")]
    public async Task<IActionResult> UpdatePassword([FromBody] UpdateProfilePasswordRequest request)
    {
        Console.WriteLine($"OldPassword: {request.OldPassword} NewPassword: {request.NewPassword} ComfirmPassword: {request.ConfirmPassword}");
        var req = new UserResetPasswordRequest
        {
            OldPassword = request.OldPassword,
            NewPassword = request.NewPassword,
            ConfirmPassword = request.ConfirmPassword,
            ProcessedById = User.GetUserId(),
            UserId = User.GetUserId(),
        };

        var result = await _userService.ResetPassword(req);

        if (result.IsSuccess)
            return Ok();
        
        var statusCode = ErrorHttpMapper.GetStatusCode(result.Error!);
        return StatusCode(statusCode, new { message = result.Error?.Message });
    }
}
