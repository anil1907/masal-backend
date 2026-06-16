using FluentValidation;

namespace Application.Features.Children.Commands.Create;

public class CreateChildCommandValidator : AbstractValidator<CreateChildCommand>
{
    public CreateChildCommandValidator()
    {
        RuleFor(c => c.HeroName)
            .NotEmpty()
            .MinimumLength(1)
            .MaximumLength(40);

        RuleForEach(c => c.Fears).MaximumLength(40);
        RuleForEach(c => c.Interests).MaximumLength(40);
        RuleFor(c => c.Fears).Must(f => f.Count <= 20);
        RuleFor(c => c.Interests).Must(i => i.Count <= 20);
    }
}
