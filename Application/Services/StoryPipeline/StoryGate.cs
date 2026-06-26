using Application.Features.Subscriptions.Constants;
using Application.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.StoryPipeline;

public record GateResult(bool CanGenerate, string? Reason);

/// Decides whether a new story may be generated now. Rules: at most 1 generation per (UTC) day;
/// free tier additionally capped at WeeklyFreeStories per rolling week; premium has no weekly cap.
/// Reason is "today" (already generated today) or "freeLimit" (free weekly allowance used up).
public class StoryGate
{
    public const string ReasonToday = "today";
    public const string ReasonFreeLimit = "freeLimit";

    private readonly IApplicationDbContext _db;

    public StoryGate(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<GateResult> EvaluateAsync(long userId, long childId, CancellationToken cancellationToken = default)
    {
        int generatedToday = await _db.StoryChapters
            .AsNoTracking()
            .CountAsync(c => c.ChildId == childId && c.CreatedDate >= DateTime.UtcNow.Date, cancellationToken);
        if (generatedToday > 0)
            return new GateResult(false, ReasonToday);

        DateTime nowUtc = DateTime.UtcNow;
        bool premium = await _db.Entitlements
            .AsNoTracking()
            .Where(e => e.UserId == userId && e.IsActive && (e.CurrentPeriodEnd == null || e.CurrentPeriodEnd > nowUtc))
            .OrderByDescending(e => e.CurrentPeriodEnd)
            .FirstOrDefaultAsync(cancellationToken) is not null;
        if (premium)
            return new GateResult(true, null);

        int thisWeek = await _db.StoryChapters
            .AsNoTracking()
            .CountAsync(c => c.ChildId == childId && c.CreatedDate >= DateTime.UtcNow.AddDays(-7), cancellationToken);
        return thisWeek < SubscriptionConstants.WeeklyFreeStories
            ? new GateResult(true, null)
            : new GateResult(false, ReasonFreeLimit);
    }
}
