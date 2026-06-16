namespace Application.Services.StoryGeneration;

/// Bound from the "StorySettings" config section.
public class StorySettings
{
    /// Per-user cap on LLM story generations per rolling 24h (every call costs real money).
    public int DailyGenerationLimit { get; set; } = 10;
}
