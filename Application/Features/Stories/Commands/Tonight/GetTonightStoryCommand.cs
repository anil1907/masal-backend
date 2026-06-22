using Application.Features.Children.Rules;
using Application.Features.Stories.Dtos;
using Application.Services.AudioStorage;
using Application.Services.CurrentUser;
using Application.Services.Repositories;
using Application.Services.StoryPipeline;
using Core.Application.Pipelines.Authorization;
using Domain.Entities.Children;
using Domain.Entities.Stories;
using MediatR;

namespace Application.Features.Stories.Commands.Tonight;

/// READ-ONLY home state for the child's active series. Generation is an explicit action
/// (POST /api/Stories/generate), capped at 1/day. No listened-tracking - we don't gate on whether
/// the child finished the story.
public class GetTonightStoryCommand : IRequest<TonightStoryResponse>, ISecuredRequest
{
    public string[] Roles => [OperationClaims.AllowAnonymous];

    public class GetTonightStoryCommandHandler : IRequestHandler<GetTonightStoryCommand, TonightStoryResponse>
    {
        private readonly IChildRepository _childRepository;
        private readonly IStoryChapterRepository _chapterRepository;
        private readonly IStorySeriesRepository _seriesRepository;
        private readonly IStoryGenerationQueue _queue;
        private readonly StoryGate _gate;
        private readonly ICurrentUser _currentUser;
        private readonly IAudioStorage _audio;
        private readonly ChildBusinessRules _childBusinessRules;

        public GetTonightStoryCommandHandler(
            IChildRepository childRepository,
            IStoryChapterRepository chapterRepository,
            IStorySeriesRepository seriesRepository,
            IStoryGenerationQueue queue,
            StoryGate gate,
            ICurrentUser currentUser,
            IAudioStorage audio,
            ChildBusinessRules childBusinessRules)
        {
            _childRepository = childRepository;
            _chapterRepository = chapterRepository;
            _seriesRepository = seriesRepository;
            _queue = queue;
            _gate = gate;
            _currentUser = currentUser;
            _audio = audio;
            _childBusinessRules = childBusinessRules;
        }

        public async Task<TonightStoryResponse> Handle(GetTonightStoryCommand request, CancellationToken cancellationToken)
        {
            long userId = _currentUser.UserIdOrThrow();
            Child? child = await _childRepository.GetActiveForUserAsync(userId, cancellationToken);
            await _childBusinessRules.ChildShouldExist(child);

            StorySeries? active = await _seriesRepository.GetActiveForChildAsync(child!.Id, cancellationToken);
            string? seriesTitle = active?.Title;

            // Generation in progress -> waiting screen.
            if (_queue.IsGenerating(child.Id))
                return new TonightStoryResponse
                {
                    Status = TonightStoryResponse.StatusPreparing,
                    CanGenerate = false,
                    SeriesTitle = seriesTitle
                };

            GateResult gate = await _gate.EvaluateAsync(userId, child.Id, cancellationToken);

            StoryChapter? latest = active is null
                ? null
                : await _chapterRepository.GetLatestForSeriesAsync(active.Id, cancellationToken);

            string status = _queue.HasRecentFailure(child.Id)
                ? TonightStoryResponse.StatusFailed
                : (latest is not null ? TonightStoryResponse.StatusReady : TonightStoryResponse.StatusEmpty);

            return new TonightStoryResponse
            {
                Status = status,
                Chapter = latest is null ? null : await ChapterDto.FromAsync(latest, _audio, cancellationToken),
                CanGenerate = gate.CanGenerate,
                BlockedReason = gate.CanGenerate ? null : gate.Reason,
                SeriesTitle = seriesTitle
            };
        }
    }
}
