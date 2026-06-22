using Application.Features.Subscriptions.Constants;
using Application.Services.CurrentUser;
using Application.Services.Repositories;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Logging;
using Domain.Entities.Subscriptions;
using MediatR;

namespace Application.Features.Subscriptions.Queries.GetStatus;

public class GetSubscriptionStatusQuery : IRequest<GetSubscriptionStatusResponse>, ISecuredRequest, ILoggableRequest
{
    public string[] Roles => [OperationClaims.AllowAnonymous];

    public class GetSubscriptionStatusQueryHandler : IRequestHandler<GetSubscriptionStatusQuery, GetSubscriptionStatusResponse>
    {
        private readonly IEntitlementRepository _entitlementRepository;
        private readonly IChildRepository _childRepository;
        private readonly IStoryChapterRepository _chapterRepository;
        private readonly ICurrentUser _currentUser;

        public GetSubscriptionStatusQueryHandler(
            IEntitlementRepository entitlementRepository,
            IChildRepository childRepository,
            IStoryChapterRepository chapterRepository,
            ICurrentUser currentUser)
        {
            _entitlementRepository = entitlementRepository;
            _childRepository = childRepository;
            _chapterRepository = chapterRepository;
            _currentUser = currentUser;
        }

        public async Task<GetSubscriptionStatusResponse> Handle(GetSubscriptionStatusQuery request, CancellationToken cancellationToken)
        {
            long userId = _currentUser.UserIdOrThrow();
            DateTime now = DateTime.UtcNow;

            Entitlement? active = await _entitlementRepository.GetActiveByUserIdAsync(userId, now, cancellationToken);

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
            var child = await _childRepository.GetActiveForUserAsync(userId, cancellationToken);
            if (child is not null)
                deliveredThisWeek = await _chapterRepository.CountForChildSinceAsync(
                    child.Id, now.AddDays(-7), cancellationToken);

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
