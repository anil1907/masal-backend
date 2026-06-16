using FluentValidation;

namespace Application.Features.Auth.Commands.SendOtp;

public class SendOtpCommandValidator : AbstractValidator<SendOtpCommand>
{
    public SendOtpCommandValidator()
    {
        RuleFor(c => c.PhoneNumber)
            .NotEmpty()
            .Matches(@"^[+]?[0-9\s\-()]{10,16}$")
            .WithMessage("Please enter a valid phone number.");
    }
}
