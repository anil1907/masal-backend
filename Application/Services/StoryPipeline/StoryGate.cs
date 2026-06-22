using Application.Features.Subscriptions.Constants;
using Application.Services.Repositories;

namespace Application.Services.StoryPipeline;

public record GateResult(bool CanGenerate, string? Reason);

/// Decides whether a new story may be generated now. Rules: at most 1 generation per (UTC) day;
/// free tier additionally capped at WeeklyFreeStories per rolling week; premium has no weekly cap.
/// Reason is "today" (already generated today) or "freeLimit" (free weekly allowance used up).
public class StoryGate
{
    public const string ReasonToday = "today";
    public const string ReasonFreeLimit = "freeLimit";

    private readonly IStoryChapterRepository _chapterRepository;
    private readonly IEntitlementRepository _entitlementRepository;

    public StoryGate(IStoryChapterRepository chapterRepository, IEntitlementRepository entitlementRepository)
    {
        _chapterRepository = chapterRepository;
        _entitlementRepository = entitlementRepository;
    }

    public async Task<GateResult> EvaluateAsync(long userId, long childId, CancellationToken cancellationToken = default)
    {
        int generatedToday = await _chapterRepository.CountForChildSinceAsync(
            childId, DateTime.UtcNow.Date, cancellationToken);
        if (generatedToday > 0)
            return new GateResult(false, ReasonToday);

        bool premium = await _entitlementRepository.GetActiveByUserIdAsync(userId, DateTime.UtcNow, cancellationToken) is not null;
        if (premium)
            return new GateResult(true, null);

        int thisWeek = await _chapterRepository.CountForChildSinceAsync(
            childId, DateTime.UtcNow.AddDays(-7), cancellationToken);
        return thisWeek < SubscriptionConstants.WeeklyFreeStories
            ? new GateResult(true, null)
            : new GateResult(false, ReasonFreeLimit);
    }
}
