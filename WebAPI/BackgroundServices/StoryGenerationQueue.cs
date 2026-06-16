using System.Collections.Concurrent;
using System.Threading.Channels;
using Application.Services.StoryPipeline;

namespace WebAPI.BackgroundServices;

/// In-memory generation queue (single Render instance). A child can have at most one job in flight;
/// a failure is remembered until the client explicitly retries or a later generation succeeds.
/// On instance restart the in-memory state is lost - harmless: an unfinished job simply re-triggers
/// on the next poll/open since no chapter was persisted.
public class StoryGenerationQueue : IStoryGenerationQueue
{
    private readonly Channel<StoryGenerationJob> _channel = Channel.CreateUnbounded<StoryGenerationJob>();
    private readonly ConcurrentDictionary<long, byte> _inProgress = new();
    private readonly ConcurrentDictionary<long, byte> _failed = new();

    public ChannelReader<StoryGenerationJob> Reader => _channel.Reader;

    public bool TryEnqueue(StoryGenerationJob job)
    {
        if (!_inProgress.TryAdd(job.ChildId, 0))
            return false;
        _failed.TryRemove(job.ChildId, out _);
        _channel.Writer.TryWrite(job);
        return true;
    }

    public bool IsGenerating(long childId) => _inProgress.ContainsKey(childId);

    public bool HasRecentFailure(long childId) => _failed.ContainsKey(childId);

    public void ClearFailure(long childId) => _failed.TryRemove(childId, out _);

    // Consumer side (background worker).
    public void MarkCompleted(long childId) => _inProgress.TryRemove(childId, out _);

    public void MarkFailed(long childId)
    {
        _inProgress.TryRemove(childId, out _);
        _failed[childId] = 0;
    }
}
