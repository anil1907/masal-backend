using Application.Features.Children.Rules;
using Application.Features.Stories.Commands.Tonight;
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
        private readonly IStoryGenerationQueue _queue;
        private readonly StoryGate _gate;
        private readonly ICurrentUser _currentUser;
        private readonly ChildBusinessRules _childBusinessRules;

        public NewStorySeriesCommandHandler(
            IChildRepository childRepository,
            IStorySeriesRepository seriesRepository,
            IStoryGenerationQueue queue,
            StoryGate gate,
            ICurrentUser currentUser,
            ChildBusinessRules childBusinessRules)
        {
            _childRepository = childRepository;
            _seriesRepository = seriesRepository;
            _queue = queue;
            _gate = gate;
            _currentUser = currentUser;
            _childBusinessRules = childBusinessRules;
        }

        public async Task<TonightStoryResponse> Handle(NewStorySeriesCommand request, CancellationToken cancellationToken)
        {
            long userId = _currentUser.UserIdOrThrow();
            Child? child = await _childRepository.GetActiveForUserAsync(userId, cancellationToken);
            await _childBusinessRules.ChildShouldExist(child);

            // Don't start a second generation while one is running.
            if (_queue.IsGenerating(child!.Id))
                return new() { Status = TonightStoryResponse.StatusPreparing, CanGenerate = false };

            // A new series is also one generation - same 1/day + weekly gate.
            GateResult gate = await _gate.EvaluateAsync(userId, child.Id, cancellationToken);
            if (!gate.CanGenerate)
                return new() { Status = TonightStoryResponse.StatusEmpty, CanGenerate = false, BlockedReason = gate.Reason };

            _queue.ClearFailure(child.Id);
            await _seriesRepository.DeactivateAllForChildAsync(child.Id, cancellationToken);
            StorySeries created = await _seriesRepository.AddAsync(
                new StorySeries { ChildId = child.Id, Title = "Yeni masal", IsActive = true }, cancellationToken);

            _queue.TryEnqueue(new StoryGenerationJob(userId, child.Id));
            return new() { Status = TonightStoryResponse.StatusPreparing, CanGenerate = false, SeriesTitle = created.Title };
        }
    }
}
