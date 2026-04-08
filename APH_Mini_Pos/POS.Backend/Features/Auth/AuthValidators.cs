using FluentValidation;

namespace POS.Backend.Features.Auth
{
    public class LoginRequestValidator : AbstractValidator<LoginRequest>
    {
        public LoginRequestValidator()
        {
            RuleFor(x => x.Username).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
        }
    }
}
