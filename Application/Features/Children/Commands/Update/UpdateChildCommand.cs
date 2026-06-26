using Application.Features.Children.Rules;
using Application.Persistence;
using Application.Services.CurrentUser;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Logging;
using Domain.Entities.Children;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Children.Commands.Update;

public class UpdateChildCommand : IRequest<UpdatedChildResponse>, ISecuredRequest, ILoggableRequest
{
    /// Which child to edit. 0 (or omitted) means "the active child" - keeps old single-child clients working.
    public long Id { get; set; }
    public string HeroName { get; set; } = default!;
    public List<string> Fears { get; set; } = [];
    public List<string> Interests { get; set; } = [];
    public string? AgeBand { get; set; }
    public string? Gender { get; set; }

    public string[] Roles => [OperationClaims.AllowAnonymous];

    public class UpdateChildCommandHandler : IRequestHandler<UpdateChildCommand, UpdatedChildResponse>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUser _currentUser;
        private readonly ChildBusinessRules _childBusinessRules;

        public UpdateChildCommandHandler(
            IApplicationDbContext db,
            ICurrentUser currentUser,
            ChildBusinessRules childBusinessRules)
        {
            _db = db;
            _currentUser = currentUser;
            _childBusinessRules = childBusinessRules;
        }

        public async Task<UpdatedChildResponse> Handle(UpdateChildCommand request, CancellationToken cancellationToken)
        {
            long userId = _currentUser.UserIdOrThrow();

            Child? child = request.Id > 0
                ? await _db.Children
                    .FirstOrDefaultAsync(c => c.Id == request.Id && c.UserId == userId, cancellationToken)
                : await _db.Children
                    .AsNoTracking()
                    .Where(c => c.UserId == userId)
                    .OrderByDescending(c => c.IsActive)
                    .ThenByDescending(c => c.Id)
                    .FirstOrDefaultAsync(cancellationToken);
            await _childBusinessRules.ChildShouldExist(child);

            child!.HeroName = request.HeroName;
            child.Fears = request.Fears;
            child.Interests = request.Interests;
            child.AgeBand = request.AgeBand;
            child.Gender = request.Gender;

            _db.Children.Update(child);
            await _db.SaveChangesAsync(cancellationToken);
            return new UpdatedChildResponse
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
