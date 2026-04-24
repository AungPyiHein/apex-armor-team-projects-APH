using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using POS.Backend.Features.Loyalty;
using POS.Backend.Common;
using System.Text.RegularExpressions;
using POS.Shared.Models;
using POS.data.Entities;

namespace POS.Backend.Features.Loyalty
{
    [ApiController]
    [Route("api/v1/loyalty")]
    public class LoyaltyController : ControllerBase
    {
        private readonly ILoyaltyServices _loyaltyServices;
        private readonly POS.data.Data.AppDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public LoyaltyController(ILoyaltyServices loyaltyServices, POS.data.Data.AppDbContext context, ICurrentUserService currentUser)
        {
            _loyaltyServices = loyaltyServices;
            _context = context;
            _currentUser = currentUser;
        }

        /// <summary>
        /// Derives the Loyalty Engine system ID as "APH_POS_{MERCHANTNAME}" (e.g. "APH_POS_UNIQUE").
        /// Each merchant must have a matching system registered on the Loyalty Engine.
        /// </summary>
        private async Task<(string? systemId, string? apiKey)> GetMerchantLoyaltyInfoAsync(Guid? merchantId = null)
        {
            var targetId = merchantId ?? _currentUser.MerchantId;
            if (!targetId.HasValue) return (null, null);

            var merchant = await _context.Merchants.FirstOrDefaultAsync(m => m.Id == targetId.Value);
            if (merchant == null) return (null, null);

            var safeName = Regex.Replace(merchant.Name.ToUpperInvariant(), @"[^A-Z0-9]", "_");
            return ($"APH_POS_{safeName}", null);
        }

        private async Task<(string? systemId, string? apiKey)> GetCustomerMerchantLoyaltyInfoAsync(Guid customerId)
        {
            var customer = await _context.Customers
                .Include(c => c.Merchant)
                .FirstOrDefaultAsync(c => c.Id == customerId);
            if (customer?.Merchant == null) return (null, null);

            var safeName = Regex.Replace(customer.Merchant.Name.ToUpperInvariant(), @"[^A-Z0-9]", "_");
            return ($"APH_POS_{safeName}", null);
        }

        private async Task<Customer?> GetCustomerWithMerchantAsync(Guid customerId)
        {
            return await _context.Customers
                .Include(c => c.Merchant)
                .FirstOrDefaultAsync(c => c.Id == customerId);
        }

        private bool IsMerchantScopedUser() =>
            _currentUser.Role == UserRole.MerchantAdmin || _currentUser.Role == UserRole.Staff;

        private bool CanAccessCustomer(Customer customer)
        {
            if (_currentUser.Role == UserRole.Admin)
            {
                return true;
            }

            return _currentUser.MerchantId.HasValue && customer.MerchantId == _currentUser.MerchantId.Value;
        }

        private async Task<(bool isAllowed, ActionResult? failure, string? systemId, string? apiKey)> ResolveCustomerScopeAsync(Guid customerId)
        {
            var customer = await GetCustomerWithMerchantAsync(customerId);
            if (customer?.Merchant == null)
            {
                return (false, BadRequest(Result<string>.Failure("Customer or merchant not found.")), null, null);
            }

            if (!CanAccessCustomer(customer))
            {
                return (false, StatusCode(StatusCodes.Status403Forbidden, Result<string>.Failure("Access denied for this customer.")), null, null);
            }

            var safeName = Regex.Replace(customer.Merchant.Name.ToUpperInvariant(), @"[^A-Z0-9]", "_");
            return (true, null, $"APH_POS_{safeName}", null);
        }

        private async Task<(bool isAllowed, ActionResult? failure, string? systemId, string? apiKey)> ResolveMerchantScopeAsync(Guid? merchantId = null)
        {
            Guid targetMerchantId;
            if (_currentUser.Role == UserRole.Admin)
            {
                if (!merchantId.HasValue || merchantId == Guid.Empty)
                {
                    return (false, BadRequest(Result<string>.Failure("merchantId is required for admin-scoped loyalty requests.")), null, null);
                }

                targetMerchantId = merchantId.Value;
            }
            else
            {
                if (!_currentUser.MerchantId.HasValue)
                {
                    return (false, BadRequest(Result<string>.Failure("Merchant context is required.")), null, null);
                }

                targetMerchantId = _currentUser.MerchantId.Value;
            }

            var merchant = await _context.Merchants.FirstOrDefaultAsync(m => m.Id == targetMerchantId);
            if (merchant == null)
            {
                return (false, NotFound(Result<string>.Failure("Merchant not found.")), null, null);
            }

            var safeName = Regex.Replace(merchant.Name.ToUpperInvariant(), @"[^A-Z0-9]", "_");
            return (true, null, $"APH_POS_{safeName}", null);
        }

        [HttpGet("customer/{customerId}")]
        [Authorize(Roles = "Admin,MerchantAdmin,Staff")]
        public async Task<ActionResult<Result<LoyaltyAccountResponse>>> GetCustomerLoyalty(Guid customerId)
        {
            var scope = await ResolveCustomerScopeAsync(customerId);
            if (!scope.isAllowed)
            {
                return scope.failure!;
            }

            var (systemId, apiKey) = (scope.systemId, scope.apiKey);
            var result = await _loyaltyServices.GetCustomerLoyaltyAsync(customerId, systemId, apiKey);
            return Ok(result);
        }

        [HttpGet("customer/{customerId}/history")]
        [Authorize(Roles = "Admin,MerchantAdmin,Staff")]
        public async Task<ActionResult<Result<List<LoyaltyHistoryDto>>>> GetCustomerHistory(Guid customerId)
        {
            var scope = await ResolveCustomerScopeAsync(customerId);
            if (!scope.isAllowed)
            {
                return scope.failure!;
            }

            var (systemId, apiKey) = (scope.systemId, scope.apiKey);
            var result = await _loyaltyServices.GetCustomerHistoryAsync(customerId, systemId, apiKey);
            return Ok(result);
        }

        [HttpGet("rewards")]
        [Authorize(Roles = "Admin,MerchantAdmin,Staff")]
        public async Task<ActionResult<Result<List<LoyaltyReward>>>> GetRewards([FromQuery] Guid? customerId = null, [FromQuery] Guid? merchantId = null)
        {
            if (customerId.HasValue && customerId != Guid.Empty)
            {
                var customerScope = await ResolveCustomerScopeAsync(customerId.Value);
                if (!customerScope.isAllowed)
                {
                    return customerScope.failure!;
                }

                var customerResult = await _loyaltyServices.GetActiveRewardsAsync(customerScope.systemId, customerScope.apiKey);
                return Ok(customerResult);
            }

            var scope = await ResolveMerchantScopeAsync(merchantId);
            if (!scope.isAllowed)
            {
                return scope.failure!;
            }

            var (systemId, apiKey) = (scope.systemId, scope.apiKey);
            var result = await _loyaltyServices.GetActiveRewardsAsync(systemId, apiKey);
            return Ok(result);
        }

        [HttpGet("rules")]
        [Authorize(Roles = "Admin,MerchantAdmin,Staff")]
        public async Task<ActionResult<Result<List<LoyaltyRuleDto>>>> GetRules()
        {
            if (IsMerchantScopedUser() && !_currentUser.MerchantId.HasValue)
            {
                return BadRequest(Result<List<LoyaltyRuleDto>>.Failure("Merchant context is required."));
            }

            var (systemId, apiKey) = await GetMerchantLoyaltyInfoAsync();
            var result = await _loyaltyServices.GetActiveRulesAsync(systemId, apiKey);
            return Ok(result);
        }

        [HttpPost("claim")]
        [Authorize(Roles = "Admin,MerchantAdmin,Staff")]
        public async Task<ActionResult<Result<bool>>> ClaimReward([FromBody] ClaimRewardRequest request)
        {
            var scope = await ResolveCustomerScopeAsync(request.CustomerId);
            if (!scope.isAllowed)
            {
                return scope.failure!;
            }

            var (systemId, apiKey) = (scope.systemId, scope.apiKey);
            var result = await _loyaltyServices.ClaimRewardAsync(request.CustomerId, request.RewardId, request.Notes, systemId, apiKey);
            return Ok(result);
        }

        [HttpGet("admin/stats")]
        [Authorize(Roles = "Admin,MerchantAdmin")]
        public async Task<ActionResult<Result<LoyaltyAdminStatsResponse>>> GetAdminStats([FromQuery] Guid? merchantId = null)
        {
            var scope = await ResolveMerchantScopeAsync(merchantId);
            if (!scope.isAllowed)
            {
                return scope.failure!;
            }

            var result = await _loyaltyServices.GetAdminStatsAsync(scope.systemId, scope.apiKey);
            return Ok(result);
        }

        [HttpGet("admin/redemptions/history")]
        [Authorize(Roles = "Admin,MerchantAdmin")]
        public async Task<ActionResult<Result<PagedRedemptionHistoryResponse>>> GetRedemptionHistory([FromQuery] Guid? merchantId = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? status = null, [FromQuery] string? searchTerm = null)
        {
            var scope = await ResolveMerchantScopeAsync(merchantId);
            if (!scope.isAllowed)
            {
                return scope.failure!;
            }

            var (systemId, apiKey) = (scope.systemId, scope.apiKey);
            var result = await _loyaltyServices.GetRedemptionHistoryAsync(page, pageSize, status, searchTerm, systemId, apiKey);
            if (result.IsSuccess && result.Value?.Items != null && result.Value.Items.Count > 0)
            {
                var customerIds = result.Value.Items
                    .Select(i => Guid.TryParse(i.ExternalUserId, out var customerId) ? customerId : Guid.Empty)
                    .Where(id => id != Guid.Empty)
                    .Distinct()
                    .ToList();

                if (customerIds.Count > 0)
                {
                    var customerLookup = await _context.Customers
                        .AsNoTracking()
                        .Where(c => customerIds.Contains(c.Id))
                        .Select(c => new { c.Id, c.Name })
                        .ToDictionaryAsync(c => c.Id, c => c.Name);

                    foreach (var item in result.Value.Items)
                    {
                        if (Guid.TryParse(item.ExternalUserId, out var customerId) && customerLookup.TryGetValue(customerId, out var customerName))
                        {
                            item.CustomerName = customerName;
                        }
                    }
                }
            }

            return Ok(result);
        }

        [HttpGet("admin/global-ledger")]
        [Authorize(Roles = "Admin,MerchantAdmin")]
        public async Task<ActionResult<Result<PagedLedgerHistoryResponse>>> GetGlobalLedger([FromQuery] Guid? merchantId = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? searchTerm = null)
        {
            var scope = await ResolveMerchantScopeAsync(merchantId);
            if (!scope.isAllowed)
            {
                return scope.failure!;
            }

            var (systemId, apiKey) = (scope.systemId, scope.apiKey);
            var result = await _loyaltyServices.GetGlobalLedgerAsync(page, pageSize, searchTerm, systemId, apiKey);
            return Ok(result);
        }

        [HttpPost("admin/redemptions/{redemptionId:guid}/fulfill")]
        [Authorize(Roles = "Admin,MerchantAdmin")]
        public async Task<ActionResult<Result<bool>>> FulfillRedemption(Guid redemptionId, [FromQuery] Guid? merchantId = null)
        {
            var scope = await ResolveMerchantScopeAsync(merchantId);
            if (!scope.isAllowed)
            {
                return scope.failure!;
            }

            var result = await _loyaltyServices.FulfillRedemptionAsync(redemptionId, scope.systemId, scope.apiKey);
            return Ok(result);
        }
    }

    public class ClaimRewardRequest
    {
        public Guid CustomerId { get; set; }
        public Guid RewardId { get; set; }
        public string? Notes { get; set; }
    }
}
