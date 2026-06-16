using FluentValidation;

namespace Application.Features.Users.Commands.Update;

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(c => c.Id)
            .NotEmpty()
            .WithMessage("Id is required.");

        RuleFor(c => c.Username)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(50)
            .WithMessage("Username must be between 3 and 50 characters.");

        RuleFor(c => c.Email)
            .NotEmpty()
            .EmailAddress()
            .WithMessage("Please enter a valid email address.");

        RuleFor(c => c.Password)
            .MinimumLength(6)
            .When(c => !string.IsNullOrEmpty(c.Password))
            .WithMessage("Password must be at least 6 characters long when provided.");
    }
}
