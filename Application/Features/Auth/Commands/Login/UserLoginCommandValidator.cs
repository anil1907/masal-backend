using FluentValidation;

namespace Application.Features.Auth.Commands.Login;

public class UserLoginCommandValidator : AbstractValidator<UserLoginCommand>
{
    public UserLoginCommandValidator()
    {
        RuleFor(c => c.UserForLoginDto).NotNull();
        RuleFor(c => c.UserForLoginDto.Username).NotEmpty();
        RuleFor(c => c.UserForLoginDto.Password).NotEmpty();
    }
}
