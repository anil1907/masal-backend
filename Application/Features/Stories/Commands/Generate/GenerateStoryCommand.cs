using Application.Features.Children.Rules;
using Application.Features.Stories.Commands.Tonight;
using Application.Features.Stories.Dtos;
using Application.Services.AudioStorage;
using Application.Services.CurrentUser;
using Application.Services.Repositories;
using Application.Services.StoryPipeline;
using Core.Application.Pipelines.Authorization;
using Domain.Entities.Children;
using Domain.Entities.Stories;
using MediatR;

namespace Application.Features.Stories.Commands.Generate;

/// Explicit "create tonight's story" action - the button. Enqueues the next chapter of the active
/// series (creating the series if none) when allowed (1/day; free weekly cap). Returns "preparing"
/// on success, or the current state with a BlockedReason if not allowed.
public class GenerateStoryCommand : IRequest<TonightStoryResponse>, ISecuredRequest
{
    public string[] Roles => [OperationClaims.AllowAnonymous];

    public class GenerateStoryCommandHandler : IRequestHandler<GenerateStoryCommand, TonightStoryResponse>
    {
        private readonly IChildRepository _childRepository;
        private readonly IStoryChapterRepository _chapterRepository;
        private readonly IStorySeriesRepository _seriesRepository;
        private readonly IStoryGenerationQueue _queue;
        private readonly StoryGate _gate;
        private readonly ICurrentUser _currentUser;
        private readonly IAudioStorage _audio;
        private readonly ChildBusinessRules _childBusinessRules;

        public GenerateStoryCommandHandler(
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

        public async Task<TonightStoryResponse> Handle(GenerateStoryCommand request, CancellationToken cancellationToken)
        {
            long userId = _currentUser.UserIdOrThrow();
            Child? child = await _childRepository.GetByUserIdAsync(userId, cancellationToken);
            await _childBusinessRules.ChildShouldExist(child);

            StorySeries? active = await _seriesRepository.GetActiveForChildAsync(child!.Id, cancellationToken);

            if (_queue.IsGenerating(child.Id))
                return new TonightStoryResponse { Status = TonightStoryResponse.StatusPreparing, CanGenerate = false, SeriesTitle = active?.Title };

            // A manual generate also clears a prior failure (it's the retry).
            _queue.ClearFailure(child.Id);

            GateResult gate = await _gate.EvaluateAsync(userId, child.Id, cancellationToken);
            if (!gate.CanGenerate)
            {
                StoryChapter? latest = active is null ? null : await _chapterRepository.GetLatestForSeriesAsync(active.Id, cancellationToken);
                return new TonightStoryResponse
                {
                    Status = latest is not null ? TonightStoryResponse.StatusReady : TonightStoryResponse.StatusEmpty,
                    Chapter = latest is null ? null : await ChapterDto.FromAsync(latest, _audio, cancellationToken),
                    CanGenerate = false,
                    BlockedReason = gate.Reason,
                    SeriesTitle = active?.Title
                };
            }

            // Ensure there is an active series for the pipeline to continue.
            if (active is null)
                active = await _seriesRepository.AddAsync(
                    new StorySeries { ChildId = child.Id, Title = "Yeni masal", IsActive = true }, cancellationToken);

            _queue.TryEnqueue(new StoryGenerationJob(userId, child.Id));
            return new TonightStoryResponse { Status = TonightStoryResponse.StatusPreparing, CanGenerate = false, SeriesTitle = active.Title };
        }
    }
}
