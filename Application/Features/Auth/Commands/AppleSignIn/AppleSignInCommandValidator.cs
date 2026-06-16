using FluentValidation;

namespace Application.Features.Auth.Commands.AppleSignIn;

public class AppleSignInCommandValidator : AbstractValidator<AppleSignInCommand>
{
    public AppleSignInCommandValidator()
    {
        RuleFor(c => c.IdentityToken).NotEmpty();
    }
}
