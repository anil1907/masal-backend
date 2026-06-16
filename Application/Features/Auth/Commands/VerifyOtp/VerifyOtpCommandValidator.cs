using FluentValidation;

namespace Application.Features.Auth.Commands.VerifyOtp;

public class VerifyOtpCommandValidator : AbstractValidator<VerifyOtpCommand>
{
    public VerifyOtpCommandValidator()
    {
        RuleFor(c => c.PhoneNumber)
            .NotEmpty()
            .Matches(@"^[+]?[0-9\s\-()]{10,16}$")
            .WithMessage("Please enter a valid phone number.");

        RuleFor(c => c.Code)
            .NotEmpty()
            .Matches(@"^[0-9]{4,8}$")
            .WithMessage("Please enter a valid verification code.");
    }
}
