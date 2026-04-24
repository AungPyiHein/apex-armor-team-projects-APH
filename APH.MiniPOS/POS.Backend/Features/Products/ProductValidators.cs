using FluentValidation;

namespace POS.Backend.Features.Products
{
    public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
    {
        public CreateProductRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Price).GreaterThan(0);
            RuleFor(x => x.SKU).NotEmpty().MaximumLength(50);
            RuleFor(x => x.CategoryId).NotEmpty();
            RuleFor(x => x.MerchantId).NotEmpty();
        }
    }

    public class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
    {
        public UpdateProductRequestValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Name).MaximumLength(100).When(x => x.Name != null);
            RuleFor(x => x.Price).GreaterThan(0).When(x => x.Price.HasValue);
            RuleFor(x => x.SKU).MaximumLength(50).When(x => x.SKU != null);
        }
    }
}
