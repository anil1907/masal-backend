using Application.Features.Children.Queries.GetList;
using Application.Features.Children.Rules;
using Application.Persistence;
using Application.Services.CurrentUser;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Logging;
using Domain.Entities.Children;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Children.Commands.Activate;

public class ActivateChildCommand : IRequest<ChildListItem>, ISecuredRequest, ILoggableRequest
{
    public long Id { get; set; }

    public string[] Roles => [OperationClaims.AllowAnonymous];

    public class ActivateChildCommandHandler : IRequestHandler<ActivateChildCommand, ChildListItem>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUser _currentUser;
        private readonly ChildBusinessRules _childBusinessRules;

        public ActivateChildCommandHandler(
            IApplicationDbContext db,
            ICurrentUser currentUser,
            ChildBusinessRules childBusinessRules)
        {
            _db = db;
            _currentUser = currentUser;
            _childBusinessRules = childBusinessRules;
        }

        public async Task<ChildListItem> Handle(ActivateChildCommand request, CancellationToken cancellationToken)
        {
            long userId = _currentUser.UserIdOrThrow();

            Child? child = await _db.Children
                .FirstOrDefaultAsync(c => c.Id == request.Id && c.UserId == userId, cancellationToken);
            await _childBusinessRules.ChildShouldExist(child);

            await _db.Children
                .Where(c => c.UserId == userId && c.IsActive)
                .ExecuteUpdateAsync(set => set.SetProperty(c => c.IsActive, false), cancellationToken);
            child!.IsActive = true;
            _db.Children.Update(child);
            await _db.SaveChangesAsync(cancellationToken);

            return new ChildListItem
            {
                Id = child.Id,
                HeroName = child.HeroName,
                Fears = child.Fears,
                Interests = child.Interests,
                AgeBand = child.AgeBand,
                Gender = child.Gender,
                IsActive = child.IsActive
            };
        }
    }
}
