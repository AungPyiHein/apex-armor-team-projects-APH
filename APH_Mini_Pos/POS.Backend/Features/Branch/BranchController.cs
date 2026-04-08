using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using POS.Backend.Features.Branch;

namespace POS.Backend.Features.Branch
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class BranchController : ControllerBase
    {
        private readonly IBranchServices _branchServices;
        public BranchController(IBranchServices branchServices)
        {
            _branchServices = branchServices;
        }

        [Authorize(Roles = "Admin,MerchantAdmin")]
        [HttpPost]
        public async Task<IActionResult> CreateBranch(CreateBranchRequest request)
        {
            var result = await _branchServices.CreateBranchAsync(request);
            return result.IsSuccess ? Ok(new { IsSuccess = true, Message = "Branch Created", Data = result.Value }) : BadRequest(new { IsSuccess = false, Message = result.Error });
        }

        [HttpGet]
        public async Task<IActionResult> GetAllBranches([FromQuery] PaginationFilter filter)
        {
            var result = await _branchServices.GetAllBranchesAsync(filter);
            return result.IsSuccess ? Ok(new { IsSuccess = true, Message = "Branches retrieved successfully", Data = result.Value }) : BadRequest(new { IsSuccess = false, Message = result.Error });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetBranchById(Guid id)
        {
            var result = await _branchServices.GetBranchByIdAsync(id);
            return result.IsSuccess ? Ok(new { IsSuccess = true, Message = "Branch retrieved successfully", Data = result.Value }) : NotFound(new { IsSuccess = false, Message = result.Error });
        }

        [HttpGet("merchant/{merchantId}")]
        public async Task<IActionResult> GetBranchesByMerchantId(Guid merchantId, [FromQuery] PaginationFilter filter)
        {
            var result = await _branchServices.GetBranchesByMerchantIdAsync(merchantId, filter);
            return result.IsSuccess ? Ok(new { IsSuccess = true, Message = "Branches retrieved successfully", Data = result.Value }) : BadRequest(new { IsSuccess = false, Message = result.Error });
        }

        [Authorize(Roles = "Admin,MerchantAdmin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBranch(Guid id, UpdateBranchRequest request)
        {
            if (id != request.Id)
            {
                return BadRequest(new { IsSuccess = false, Message = "ID mismatch" });
            }
            var result = await _branchServices.UpdateBranchAsync(request);
            return result.IsSuccess ? Ok(new { IsSuccess = true, Message = "Branch Updated" }) : BadRequest(new { IsSuccess = false, Message = result.Error });
        }

        [Authorize(Roles = "Admin,MerchantAdmin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBranch(Guid id)
        {
            var result = await _branchServices.DeleteBranchAsync(id);
            return result.IsSuccess ? Ok(new { IsSuccess = true, Message = "Branch Deleted" }) : BadRequest(new { IsSuccess = false, Message = result.Error });
        }

        [Authorize(Roles = "Admin,MerchantAdmin")]
        [HttpPatch("{id}/restore")]
        public async Task<IActionResult> RestoreBranch(Guid id)
        {
            var result = await _branchServices.RestoreBranchAsync(id);
            return result.IsSuccess ? Ok(new { IsSuccess = true, Message = "Branch Restored" }) : BadRequest(new { IsSuccess = false, Message = result.Error });
        }
    }
}
