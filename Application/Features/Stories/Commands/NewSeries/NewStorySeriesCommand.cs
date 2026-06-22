using Application.Features.Children.Rules;
using Application.Features.Stories.Commands.Tonight;
using Application.Features.Subscriptions.Constants;
using Application.Services.CurrentUser;
using Application.Services.Repositories;
using Application.Services.StoryPipeline;
using Core.Application.Pipelines.Authorization;
using Domain.Entities.Children;
using Domain.Entities.Stories;
using MediatR;

namespace Application.Features.Stories.Commands.NewSeries;

/// Start a brand-new story series (when the child is bored of the current one). Deactivates the
/// current series (it stays resumable), creates a fresh active series, and enqueues its first
/// chapter. Returns the tonight state ("preparing", or "freeLimitReached" if out of free budget).
/// Deliberate user action, so it is NOT blocked by the 1/day rule - only by the weekly free cap.
public class NewStorySeriesCommand : IRequest<TonightStoryResponse>, ISecuredRequest
{
    public string[] Roles => [OperationClaims.AllowAnonymous];

    public class NewStorySeriesCommandHandler : IRequestHandler<NewStorySeriesCommand, TonightStoryResponse>
    {
        private readonly IChildRepository _childRepository;
        private readonly IStorySeriesRepository _seriesRepository;
        private readonly IStoryChapterRepository _chapterRepository;
        private readonly IEntitlementRepository _entitlementRepository;
        private readonly IStoryGenerationQueue _queue;
        private readonly ICurrentUser _currentUser;
        private readonly ChildBusinessRules _childBusinessRules;

        public NewStorySeriesCommandHandler(
            IChildRepository childRepository,
            IStorySeriesRepository seriesRepository,
            IStoryChapterRepository chapterRepository,
            IEntitlementRepository entitlementRepository,
            IStoryGenerationQueue queue,
            ICurrentUser currentUser,
            ChildBusinessRules childBusinessRules)
        {
            _childRepository = childRepository;
            _seriesRepository = seriesRepository;
            _chapterRepository = chapterRepository;
            _entitlementRepository = entitlementRepository;
            _queue = queue;
            _currentUser = currentUser;
            _childBusinessRules = childBusinessRules;
        }

        public async Task<TonightStoryResponse> Handle(NewStorySeriesCommand request, CancellationToken cancellationToken)
        {
            long userId = _currentUser.UserIdOrThrow();
            Child? child = await _childRepository.GetByUserIdAsync(userId, cancellationToken);
            await _childBusinessRules.ChildShouldExist(child);

            // Don't start a second generation while one is running.
            if (_queue.IsGenerating(child!.Id))
                return new() { Status = TonightStoryResponse.StatusPreparing, Available = false };

            bool premium = await _entitlementRepository.GetActiveByUserIdAsync(userId, DateTime.UtcNow, cancellationToken) is not null;
            int thisWeek = await _chapterRepository.CountForChildSinceAsync(child.Id, DateTime.UtcNow.AddDays(-7), cancellationToken);
            bool canGenerateNew = premium || thisWeek < SubscriptionConstants.WeeklyFreeStories;
            if (!canGenerateNew)
                return new() { Status = TonightStoryResponse.StatusFreeLimitReached, Available = false };

            await _seriesRepository.DeactivateAllForChildAsync(child.Id, cancellationToken);
            StorySeries created = await _seriesRepository.AddAsync(
                new StorySeries { ChildId = child.Id, Title = "Yeni masal", IsActive = true }, cancellationToken);

            _queue.TryEnqueue(new StoryGenerationJob(userId, child.Id));
            return new() { Status = TonightStoryResponse.StatusPreparing, Available = false, SeriesTitle = created.Title };
        }
    }
}
