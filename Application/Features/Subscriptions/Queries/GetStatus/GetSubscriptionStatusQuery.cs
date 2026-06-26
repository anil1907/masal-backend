using Application.Features.Subscriptions.Constants;
using Application.Persistence;
using Application.Services.CurrentUser;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Logging;
using Domain.Entities.Subscriptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Subscriptions.Queries.GetStatus;

public class GetSubscriptionStatusQuery : IRequest<GetSubscriptionStatusResponse>, ISecuredRequest, ILoggableRequest
{
    public string[] Roles => [OperationClaims.AllowAnonymous];

    public class GetSubscriptionStatusQueryHandler : IRequestHandler<GetSubscriptionStatusQuery, GetSubscriptionStatusResponse>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUser _currentUser;

        public GetSubscriptionStatusQueryHandler(
            IApplicationDbContext db,
            ICurrentUser currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async Task<GetSubscriptionStatusResponse> Handle(GetSubscriptionStatusQuery request, CancellationToken cancellationToken)
        {
            long userId = _currentUser.UserIdOrThrow();
            DateTime now = DateTime.UtcNow;

            Entitlement? active = await _db.Entitlements
                .AsNoTracking()
                .Where(e => e.UserId == userId && e.IsActive && (e.CurrentPeriodEnd == null || e.CurrentPeriodEnd > now))
                .OrderByDescending(e => e.CurrentPeriodEnd)
                .FirstOrDefaultAsync(cancellationToken);

            if (active != null)
            {
                return new GetSubscriptionStatusResponse
                {
                    IsPremium = true,
                    RenewsAt = active.CurrentPeriodEnd,
                    WeeklyFreeLimit = SubscriptionConstants.WeeklyFreeStories,
                    RemainingThisWeek = 0
                };
            }

            // Free tier: remaining = weekly limit minus chapters delivered in the rolling week.
            int deliveredThisWeek = 0;
            // Prefer the flagged-active child; fall back to the newest one so legacy single-child
            // accounts (created before IsActive existed) still resolve a hero.
            var child = await _db.Children
                .AsNoTracking()
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.IsActive)
                .ThenByDescending(c => c.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (child is not null)
            {
                DateTime sinceUtc = now.AddDays(-7);
                deliveredThisWeek = await _db.StoryChapters
                    .AsNoTracking()
                    .CountAsync(c => c.ChildId == child.Id && c.CreatedDate >= sinceUtc, cancellationToken);
            }

            int remaining = Math.Max(0, SubscriptionConstants.WeeklyFreeStories - deliveredThisWeek);
            return new GetSubscriptionStatusResponse
            {
                IsPremium = false,
                RenewsAt = null,
                WeeklyFreeLimit = SubscriptionConstants.WeeklyFreeStories,
                RemainingThisWeek = remaining
            };
        }
    }
}
