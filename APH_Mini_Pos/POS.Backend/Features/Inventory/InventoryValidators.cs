using FluentValidation;

namespace POS.Backend.Features.Inventory
{
    public class UpdateStockRequestValidator : AbstractValidator<UpdateStockRequest>
    {
        public UpdateStockRequestValidator()
        {
            RuleFor(x => x.BranchId).NotEmpty();
            RuleFor(x => x.ProductId).NotEmpty();
            RuleFor(x => x.QuantityChange).NotEqual(0);
        }
    }
}
