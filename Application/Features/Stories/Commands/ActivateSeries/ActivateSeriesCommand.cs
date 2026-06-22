using Application.Features.Children.Rules;
using Application.Services.CurrentUser;
using Application.Services.Repositories;
using Core.Application.Pipelines.Authorization;
using Core.Application.Responses;
using Core.CrossCuttingConcerns.Exception.Types;
using Domain.Entities.Children;
using Domain.Entities.Stories;
using MediatR;

namespace Application.Features.Stories.Commands.ActivateSeries;

public class ActivateSeriesResponse : IResponse
{
    public bool Activated { get; set; }
}

/// Resume a paused series: make it the active one so tonight continues it.
public class ActivateSeriesCommand : IRequest<ActivateSeriesResponse>, ISecuredRequest
{
    public long SeriesId { get; set; }

    public string[] Roles => [OperationClaims.AllowAnonymous];

    public class ActivateSeriesCommandHandler : IRequestHandler<ActivateSeriesCommand, ActivateSeriesResponse>
    {
        private readonly IChildRepository _childRepository;
        private readonly IStorySeriesRepository _seriesRepository;
        private readonly ICurrentUser _currentUser;
        private readonly ChildBusinessRules _childBusinessRules;

        public ActivateSeriesCommandHandler(
            IChildRepository childRepository,
            IStorySeriesRepository seriesRepository,
            ICurrentUser currentUser,
            ChildBusinessRules childBusinessRules)
        {
            _childRepository = childRepository;
            _seriesRepository = seriesRepository;
            _currentUser = currentUser;
            _childBusinessRules = childBusinessRules;
        }

        public async Task<ActivateSeriesResponse> Handle(ActivateSeriesCommand request, CancellationToken cancellationToken)
        {
            long userId = _currentUser.UserIdOrThrow();
            Child? child = await _childRepository.GetActiveForUserAsync(userId, cancellationToken);
            await _childBusinessRules.ChildShouldExist(child);

            StorySeries? series = await _seriesRepository.GetForChildAsync(request.SeriesId, child!.Id, cancellationToken);
            if (series is null)
                throw new BusinessException("Masal serisi bulunamadı.");

            await _seriesRepository.DeactivateAllForChildAsync(child.Id, cancellationToken);
            series.IsActive = true;
            await _seriesRepository.UpdateAsync(series, cancellationToken);

            return new ActivateSeriesResponse { Activated = true };
        }
    }
}
