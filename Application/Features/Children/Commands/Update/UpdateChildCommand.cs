using Application.Features.Children.Rules;
using Application.Services.CurrentUser;
using Application.Services.Repositories;
using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Logging;
using Domain.Entities.Children;
using MediatR;

namespace Application.Features.Children.Commands.Update;

public class UpdateChildCommand : IRequest<UpdatedChildResponse>, ISecuredRequest, ILoggableRequest
{
    public string HeroName { get; set; } = default!;
    public List<string> Fears { get; set; } = [];
    public List<string> Interests { get; set; } = [];
    public string? AgeBand { get; set; }

    public string[] Roles => [OperationClaims.AllowAnonymous];

    public class UpdateChildCommandHandler : IRequestHandler<UpdateChildCommand, UpdatedChildResponse>
    {
        private readonly IChildRepository _childRepository;
        private readonly ICurrentUser _currentUser;
        private readonly IMapper _mapper;
        private readonly ChildBusinessRules _childBusinessRules;

        public UpdateChildCommandHandler(
            IChildRepository childRepository,
            ICurrentUser currentUser,
            IMapper mapper,
            ChildBusinessRules childBusinessRules)
        {
            _childRepository = childRepository;
            _currentUser = currentUser;
            _mapper = mapper;
            _childBusinessRules = childBusinessRules;
        }

        public async Task<UpdatedChildResponse> Handle(UpdateChildCommand request, CancellationToken cancellationToken)
        {
            long userId = _currentUser.UserIdOrThrow();

            Child? child = await _childRepository.GetByUserIdAsync(userId, cancellationToken);
            await _childBusinessRules.ChildShouldExist(child);

            child!.HeroName = request.HeroName;
            child.Fears = request.Fears;
            child.Interests = request.Interests;
            child.AgeBand = request.AgeBand;

            await _childRepository.UpdateAsync(child, cancellationToken);
            return _mapper.Map<UpdatedChildResponse>(child);
        }
    }
}
