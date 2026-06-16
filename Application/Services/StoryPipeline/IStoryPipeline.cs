namespace Application.Services.StoryPipeline;

/// Runs the heavy story pipeline for the child's NEXT chapter (continuing the arc):
/// generate (LLM) -> safety gate -> narrate (TTS) -> upload to R2 -> persist.
/// Invoked by the background worker so the HTTP request returns immediately. Throws on safety
/// failure or any generation error.
public interface IStoryPipeline
{
    Task GenerateNextChapterAsync(long userId, long childId, CancellationToken cancellationToken = default);
}
