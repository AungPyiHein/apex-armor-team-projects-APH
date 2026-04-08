using FluentValidation;

namespace POS.Backend.Features.Customers
{
    public class CreateCustomerRequestValidator : AbstractValidator<CreateCustomerRequest>
    {
        public CreateCustomerRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.MerchantId).NotEmpty();
            RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
            RuleFor(x => x.PhoneNumber).MaximumLength(20);
        }
    }
}
