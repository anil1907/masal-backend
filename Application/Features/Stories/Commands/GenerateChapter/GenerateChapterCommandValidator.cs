using FluentValidation;

namespace Application.Features.Stories.Commands.GenerateChapter;

public class GenerateChapterCommandValidator : AbstractValidator<GenerateChapterCommand>
{
    public GenerateChapterCommandValidator()
    {
        RuleFor(c => c.ChapterNumber).InclusiveBetween(1, 10_000);
        // Client-supplied text that flows into the LLM prompt - cap it hard.
        // (This dev endpoint goes away once the server persists arc summaries itself.)
        RuleFor(c => c.PreviousSummary).MaximumLength(600);
    }
}
