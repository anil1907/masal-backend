namespace Application.Services.StoryPipeline;

public readonly record struct StoryGenerationJob(long UserId, long ChildId);

/// In-memory queue + per-child status for background story generation. The producer side is used
/// by the tonight endpoint; the consumer side (channel reader, mark completed/failed) is on the
/// concrete implementation used by the background worker.
public interface IStoryGenerationQueue
{
    /// Enqueue a generation for the child unless one is already in progress.
    /// Returns false if a job for this child is already queued/running.
    bool TryEnqueue(StoryGenerationJob job);

    bool IsGenerating(long childId);

    /// True if the last generation for this child failed and hasn't been retried/cleared.
    bool HasRecentFailure(long childId);

    void ClearFailure(long childId);
}
