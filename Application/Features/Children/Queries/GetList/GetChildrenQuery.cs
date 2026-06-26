using Application.Persistence;
using Application.Services.CurrentUser;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Logging;
using Domain.Entities.Children;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Children.Queries.GetList;

public class GetChildrenQuery : IRequest<GetChildrenResponse>, ISecuredRequest, ILoggableRequest
{
    public string[] Roles => [OperationClaims.AllowAnonymous];

    public class GetChildrenQueryHandler : IRequestHandler<GetChildrenQuery, GetChildrenResponse>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUser _currentUser;

        public GetChildrenQueryHandler(IApplicationDbContext db, ICurrentUser currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async Task<GetChildrenResponse> Handle(GetChildrenQuery request, CancellationToken cancellationToken)
        {
            long userId = _currentUser.UserIdOrThrow();
            List<Child> children = await _db.Children
                .AsNoTracking()
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.Id)
                .ToListAsync(cancellationToken);
            return new GetChildrenResponse
            {
                Children = children.Select(c => new ChildListItem
                {
                    Id = c.Id,
                    HeroName = c.HeroName,
                    Fears = c.Fears,
                    Interests = c.Interests,
                    AgeBand = c.AgeBand,
                    Gender = c.Gender,
                    IsActive = c.IsActive
                }).ToList()
            };
        }
    }
}
