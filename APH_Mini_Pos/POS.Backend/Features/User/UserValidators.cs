using FluentValidation;

namespace POS.Backend.Features.User
{
    public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
    {
        public CreateUserRequestValidator()
        {
            RuleFor(x => x.Username).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(100);
            RuleFor(x => x.PlainPassword).NotEmpty().MinimumLength(6);
            RuleFor(x => x.Role).IsInEnum();
        }
    }
}
