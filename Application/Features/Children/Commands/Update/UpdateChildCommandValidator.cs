using FluentValidation;

namespace Application.Features.Children.Commands.Update;

public class UpdateChildCommandValidator : AbstractValidator<UpdateChildCommand>
{
    public UpdateChildCommandValidator()
    {
        RuleFor(c => c.HeroName).NotEmpty().MaximumLength(40);
        RuleForEach(c => c.Fears).MaximumLength(40);
        RuleForEach(c => c.Interests).MaximumLength(40);
        RuleFor(c => c.Fears).Must(f => f.Count <= 20);
        RuleFor(c => c.Interests).Must(i => i.Count <= 20);
    }
}
