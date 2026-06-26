using Application.Features.Children.Rules;
using Application.Persistence;
using Application.Services.CurrentUser;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Logging;
using Domain.Entities.Children;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Children.Commands.Create;

public class CreateChildCommand : IRequest<CreatedChildResponse>, ISecuredRequest, ILoggableRequest
{
    public string HeroName { get; set; } = default!;
    public List<string> Fears { get; set; } = [];
    public List<string> Interests { get; set; } = [];
    public string? AgeBand { get; set; }
    public string? Gender { get; set; }

    // Authenticated, any user (phone-OTP users have no operation claims).
    public string[] Roles => [OperationClaims.AllowAnonymous];

    public class CreateChildCommandHandler : IRequestHandler<CreateChildCommand, CreatedChildResponse>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUser _currentUser;
        private readonly ChildBusinessRules _childBusinessRules;

        public CreateChildCommandHandler(
            IApplicationDbContext db,
            ICurrentUser currentUser,
            ChildBusinessRules childBusinessRules)
        {
            _db = db;
            _currentUser = currentUser;
            _childBusinessRules = childBusinessRules;
        }

        public async Task<CreatedChildResponse> Handle(CreateChildCommand request, CancellationToken cancellationToken)
        {
            long userId = _currentUser.UserIdOrThrow();

            int currentCount = await _db.Children.CountAsync(c => c.UserId == userId, cancellationToken);

            DateTime nowUtc = DateTime.UtcNow;
            bool isPremium = await _db.Entitlements
                .AsNoTracking()
                .Where(e => e.UserId == userId && e.IsActive && (e.CurrentPeriodEnd == null || e.CurrentPeriodEnd > nowUtc))
                .OrderByDescending(e => e.CurrentPeriodEnd)
                .FirstOrDefaultAsync(cancellationToken) != null;
            await _childBusinessRules.UserCanAddChild(currentCount, isPremium);

            // The freshly added child becomes the active hero.
            await _db.Children
                .Where(c => c.UserId == userId && c.IsActive)
                .ExecuteUpdateAsync(set => set.SetProperty(c => c.IsActive, false), cancellationToken);

            Child child = new()
            {
                HeroName = request.HeroName,
                Fears = request.Fears,
                Interests = request.Interests,
                AgeBand = request.AgeBand,
                Gender = request.Gender,
                UserId = userId,
                IsActive = true
            };

            _db.Children.Add(child);
            await _db.SaveChangesAsync(cancellationToken);
            return new CreatedChildResponse
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
