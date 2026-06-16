namespace Application.Services.StoryGeneration;

/// Inputs for one nightly chapter. PreviousSummary is null for chapter 1.
public record StoryGenerationInput(
    string HeroName,
    IReadOnlyList<string> Fears,
    IReadOnlyList<string> Interests,
    string? AgeBand,
    int ChapterNumber,
    string? PreviousSummary
);

/// A generated chapter plus a fresh running summary to resume from next night.
public record GeneratedChapter(
    string Title,
    string Text,
    string Summary
);

/// LLM story generation (Anthropic/Claude). The output MUST still pass IStorySafetyGate
/// before it ever reaches a child.
public interface IStoryGenerator
{
    Task<GeneratedChapter> GenerateAsync(StoryGenerationInput input, CancellationToken cancellationToken = default);
}
