using FluentValidation;
using Library.Api.Dtos.Auth;

namespace Library.Api.Validation.Auth;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .Must(s => !string.IsNullOrWhiteSpace(s))
            .WithMessage("'Email' must not be empty.")
            .EmailAddress();

        RuleFor(x => x.Password)
            .Must(s => !string.IsNullOrWhiteSpace(s))
            .WithMessage("'Password' must not be empty.")
            .MinimumLength(8);
    }
}


