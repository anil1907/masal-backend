using Application.Features.Children.Rules;
using Application.Persistence;
using Application.Services.CurrentUser;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Logging;
using Domain.Entities.Children;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Children.Queries.GetMy;

public class GetMyChildQuery : IRequest<GetMyChildResponse>, ISecuredRequest, ILoggableRequest
{
    public string[] Roles => [OperationClaims.AllowAnonymous];

    public class GetMyChildQueryHandler : IRequestHandler<GetMyChildQuery, GetMyChildResponse>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUser _currentUser;
        private readonly ChildBusinessRules _childBusinessRules;

        public GetMyChildQueryHandler(
            IApplicationDbContext db,
            ICurrentUser currentUser,
            ChildBusinessRules childBusinessRules)
        {
            _db = db;
            _currentUser = currentUser;
            _childBusinessRules = childBusinessRules;
        }

        public async Task<GetMyChildResponse> Handle(GetMyChildQuery request, CancellationToken cancellationToken)
        {
            long userId = _currentUser.UserIdOrThrow();
            Child? child = await _db.Children
                .AsNoTracking()
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.IsActive)
                .ThenByDescending(c => c.Id)
                .FirstOrDefaultAsync(cancellationToken);
            await _childBusinessRules.ChildShouldExist(child);
            return new GetMyChildResponse
            {
                Id = child!.Id,
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
