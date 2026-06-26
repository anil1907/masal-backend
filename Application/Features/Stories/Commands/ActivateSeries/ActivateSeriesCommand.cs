using Application.Features.Children.Rules;
using Application.Persistence;
using Application.Services.CurrentUser;
using Core.Application.Pipelines.Authorization;
using Core.Application.Responses;
using Core.CrossCuttingConcerns.Exception.Types;
using Domain.Entities.Children;
using Domain.Entities.Stories;
using MediatR;
using Microsoft.EntityFrameworkCore;

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
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUser _currentUser;
        private readonly ChildBusinessRules _childBusinessRules;

        public ActivateSeriesCommandHandler(
            IApplicationDbContext db,
            ICurrentUser currentUser,
            ChildBusinessRules childBusinessRules)
        {
            _db = db;
            _currentUser = currentUser;
            _childBusinessRules = childBusinessRules;
        }

        public async Task<ActivateSeriesResponse> Handle(ActivateSeriesCommand request, CancellationToken cancellationToken)
        {
            long userId = _currentUser.UserIdOrThrow();
            Child? child = await _db.Children
                .AsNoTracking()
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.IsActive)
                .ThenByDescending(c => c.Id)
                .FirstOrDefaultAsync(cancellationToken);
            await _childBusinessRules.ChildShouldExist(child);

            StorySeries? series = await _db.StorySeries
                .FirstOrDefaultAsync(s => s.Id == request.SeriesId && s.ChildId == child!.Id, cancellationToken);
            if (series is null)
                throw new BusinessException("Masal serisi bulunamadı.");

            await _db.StorySeries
                .Where(s => s.ChildId == child!.Id && s.IsActive)
                .ExecuteUpdateAsync(set => set.SetProperty(s => s.IsActive, false), cancellationToken);
            series.IsActive = true;
            _db.StorySeries.Update(series);
            await _db.SaveChangesAsync(cancellationToken);

            return new ActivateSeriesResponse { Activated = true };
        }
    }
}
