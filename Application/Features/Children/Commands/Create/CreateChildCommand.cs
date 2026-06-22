using Application.Features.Children.Rules;
using Application.Services.CurrentUser;
using Application.Services.Repositories;
using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Logging;
using Domain.Entities.Children;
using MediatR;

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
        private readonly IChildRepository _childRepository;
        private readonly IEntitlementRepository _entitlementRepository;
        private readonly ICurrentUser _currentUser;
        private readonly IMapper _mapper;
        private readonly ChildBusinessRules _childBusinessRules;

        public CreateChildCommandHandler(
            IChildRepository childRepository,
            IEntitlementRepository entitlementRepository,
            ICurrentUser currentUser,
            IMapper mapper,
            ChildBusinessRules childBusinessRules)
        {
            _childRepository = childRepository;
            _entitlementRepository = entitlementRepository;
            _currentUser = currentUser;
            _mapper = mapper;
            _childBusinessRules = childBusinessRules;
        }

        public async Task<CreatedChildResponse> Handle(CreateChildCommand request, CancellationToken cancellationToken)
        {
            long userId = _currentUser.UserIdOrThrow();

            int currentCount = await _childRepository.CountForUserAsync(userId, cancellationToken);
            bool isPremium = await _entitlementRepository.GetActiveByUserIdAsync(userId, DateTime.UtcNow, cancellationToken) != null;
            await _childBusinessRules.UserCanAddChild(currentCount, isPremium);

            // The freshly added child becomes the active hero.
            await _childRepository.DeactivateAllForUserAsync(userId, cancellationToken);

            Child child = _mapper.Map<Child>(request);
            child.UserId = userId;
            child.IsActive = true;

            Child created = await _childRepository.AddAsync(child, cancellationToken);
            return _mapper.Map<CreatedChildResponse>(created);
        }
    }
}
