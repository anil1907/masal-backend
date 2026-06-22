using Application.Features.Children.Rules;
using Application.Features.Stories.Dtos;
using Application.Features.Subscriptions.Constants;
using Application.Services.AudioStorage;
using Application.Services.CurrentUser;
using Application.Services.Repositories;
using Application.Services.StoryPipeline;
using Core.Application.Pipelines.Authorization;
using Domain.Entities.Children;
using Domain.Entities.Stories;
using MediatR;

namespace Application.Features.Stories.Commands.Tonight;

/// The home-screen story state for the child's ACTIVE series. Returns immediately; heavy generation
/// runs in the background queue. Statuses: ready / comeBackTomorrow (1/day) / freeLimitReached /
/// preparing (poll) / failed (retry). Subscription gate: premium = daily arc; free = WeeklyFreeStories.
public class GetTonightStoryCommand : IRequest<TonightStoryResponse>, ISecuredRequest
{
    /// Set by the client's manual "try again" to clear a recorded failure and re-enqueue.
    public bool Retry { get; set; }

    public string[] Roles => [OperationClaims.AllowAnonymous];

    public class GetTonightStoryCommandHandler : IRequestHandler<GetTonightStoryCommand, TonightStoryResponse>
    {
        private readonly IChildRepository _childRepository;
        private readonly IStoryChapterRepository _chapterRepository;
        private readonly IStorySeriesRepository _seriesRepository;
        private readonly IEntitlementRepository _entitlementRepository;
        private readonly IStoryGenerationQueue _queue;
        private readonly ICurrentUser _currentUser;
        private readonly IAudioStorage _audio;
        private readonly ChildBusinessRules _childBusinessRules;

        public GetTonightStoryCommandHandler(
            IChildRepository childRepository,
            IStoryChapterRepository chapterRepository,
            IStorySeriesRepository seriesRepository,
            IEntitlementRepository entitlementRepository,
            IStoryGenerationQueue queue,
            ICurrentUser currentUser,
            IAudioStorage audio,
            ChildBusinessRules childBusinessRules)
        {
            _childRepository = childRepository;
            _chapterRepository = chapterRepository;
            _seriesRepository = seriesRepository;
            _entitlementRepository = entitlementRepository;
            _queue = queue;
            _currentUser = currentUser;
            _audio = audio;
            _childBusinessRules = childBusinessRules;
        }

        public async Task<TonightStoryResponse> Handle(GetTonightStoryCommand request, CancellationToken cancellationToken)
        {
            long userId = _currentUser.UserIdOrThrow();
            Child? child = await _childRepository.GetByUserIdAsync(userId, cancellationToken);
            await _childBusinessRules.ChildShouldExist(child);

            StorySeries? active = await _seriesRepository.GetActiveForChildAsync(child!.Id, cancellationToken);
            string? seriesTitle = active?.Title;

            // A job is already running -> keep the client polling.
            if (_queue.IsGenerating(child.Id))
                return Preparing(seriesTitle);

            // Surface a prior failure so the client's poll loop stops; an explicit Retry clears it.
            if (request.Retry)
                _queue.ClearFailure(child.Id);
            else if (_queue.HasRecentFailure(child.Id))
                return Failed(seriesTitle);

            bool premium = await _entitlementRepository.GetActiveByUserIdAsync(userId, DateTime.UtcNow, cancellationToken) is not null;
            bool canGenerateNew = premium || await IsUnderFreeWeeklyLimitAsync(child.Id, cancellationToken);

            // No active series yet -> first story ever: create the series and generate chapter 1.
            if (active is null)
            {
                if (!canGenerateNew)
                    return await FreeLimitReachedAsync(null, null, cancellationToken);
                StorySeries created = await _seriesRepository.AddAsync(
                    new StorySeries { ChildId = child.Id, Title = "Yeni masal", IsActive = true }, cancellationToken);
                _queue.TryEnqueue(new StoryGenerationJob(userId, child.Id));
                return Preparing(created.Title);
            }

            StoryChapter? latest = await _chapterRepository.GetLatestForSeriesAsync(active.Id, cancellationToken);

            // Active series exists but has no chapter yet (generation pending/failed earlier).
            if (latest is null)
            {
                if (!canGenerateNew)
                    return await FreeLimitReachedAsync(null, seriesTitle, cancellationToken);
                _queue.TryEnqueue(new StoryGenerationJob(userId, child.Id));
                return Preparing(seriesTitle);
            }

            // Current chapter not finished yet - serve it (no new cost).
            if (latest.ListenedDate is null)
                return await ReadyAsync(latest, seriesTitle, cancellationToken);

            // Listened. One story per day: the next unlocks the day after it was heard.
            DateTime today = DateTime.UtcNow.Date;
            if (latest.ListenedDate.Value.Date >= today)
                return await ComeBackTomorrowAsync(latest, seriesTitle, cancellationToken);

            // New day and previous heard. Free tier must still have weekly budget.
            if (!canGenerateNew)
                return await FreeLimitReachedAsync(latest, seriesTitle, cancellationToken);

            _queue.TryEnqueue(new StoryGenerationJob(userId, child.Id));
            return Preparing(seriesTitle);
        }

        private async Task<bool> IsUnderFreeWeeklyLimitAsync(long childId, CancellationToken ct)
        {
            int thisWeek = await _chapterRepository.CountForChildSinceAsync(
                childId, DateTime.UtcNow.AddDays(-7), ct);
            return thisWeek < SubscriptionConstants.WeeklyFreeStories;
        }

        private static TonightStoryResponse Preparing(string? seriesTitle)
            => new() { Status = TonightStoryResponse.StatusPreparing, Available = false, Chapter = null, SeriesTitle = seriesTitle };

        private static TonightStoryResponse Failed(string? seriesTitle)
            => new() { Status = TonightStoryResponse.StatusFailed, Available = false, Chapter = null, SeriesTitle = seriesTitle };

        private async Task<TonightStoryResponse> ReadyAsync(StoryChapter chapter, string? seriesTitle, CancellationToken ct)
            => new()
            {
                Status = TonightStoryResponse.StatusReady,
                Available = true,
                Chapter = await ChapterDto.FromAsync(chapter, _audio, ct),
                SeriesTitle = seriesTitle
            };

        private async Task<TonightStoryResponse> ComeBackTomorrowAsync(StoryChapter last, string? seriesTitle, CancellationToken ct)
            => new()
            {
                Status = TonightStoryResponse.StatusComeBackTomorrow,
                Available = false,
                Chapter = await ChapterDto.FromAsync(last, _audio, ct),
                SeriesTitle = seriesTitle
            };

        private async Task<TonightStoryResponse> FreeLimitReachedAsync(StoryChapter? last, string? seriesTitle, CancellationToken ct)
            => new()
            {
                Status = TonightStoryResponse.StatusFreeLimitReached,
                Available = false,
                Chapter = last is null ? null : await ChapterDto.FromAsync(last, _audio, ct),
                SeriesTitle = seriesTitle
            };
    }
}
