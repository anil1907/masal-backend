using Application.Services.StoryPipeline;

namespace WebAPI.BackgroundServices;

/// Drains the generation queue, running the (slow) story pipeline off the request thread so the
/// `tonight` endpoint returns immediately. Each job runs in its own DI scope.
public class StoryGenerationBackgroundService : BackgroundService
{
    private readonly StoryGenerationQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StoryGenerationBackgroundService> _logger;

    public StoryGenerationBackgroundService(
        StoryGenerationQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<StoryGenerationBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (StoryGenerationJob job in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using IServiceScope scope = _scopeFactory.CreateScope();
                IStoryPipeline pipeline = scope.ServiceProvider.GetRequiredService<IStoryPipeline>();
                await pipeline.GenerateNextChapterAsync(job.UserId, job.ChildId, stoppingToken);
                _queue.MarkCompleted(job.ChildId);
                _logger.LogInformation("Story generated for child {ChildId}", job.ChildId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Story generation failed for child {ChildId}", job.ChildId);
                _queue.MarkFailed(job.ChildId);
            }
        }
    }
}
