using FluentValidation;

namespace Application.Features.Stories.Commands.SynthesizePreview;

public class SynthesizePreviewCommandValidator : AbstractValidator<SynthesizePreviewCommand>
{
    public SynthesizePreviewCommandValidator()
    {
        // Cap per-call cost (TTS bills per character).
        RuleFor(c => c.Text).NotEmpty().MaximumLength(3000);
    }
}
