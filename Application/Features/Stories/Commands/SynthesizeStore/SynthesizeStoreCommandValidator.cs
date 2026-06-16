using FluentValidation;

namespace Application.Features.Stories.Commands.SynthesizeStore;

public class SynthesizeStoreCommandValidator : AbstractValidator<SynthesizeStoreCommand>
{
    public SynthesizeStoreCommandValidator()
    {
        // Cap per-call cost (TTS bills per character).
        RuleFor(c => c.Text).NotEmpty().MaximumLength(3000);
    }
}
