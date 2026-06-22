using Application.Features.Children.Queries.GetList;
using Application.Features.Children.Rules;
using Application.Services.CurrentUser;
using Application.Services.Repositories;
using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Logging;
using Domain.Entities.Children;
using MediatR;

namespace Application.Features.Children.Commands.Activate;

public class ActivateChildCommand : IRequest<ChildListItem>, ISecuredRequest, ILoggableRequest
{
    public long Id { get; set; }

    public string[] Roles => [OperationClaims.AllowAnonymous];

    public class ActivateChildCommandHandler : IRequestHandler<ActivateChildCommand, ChildListItem>
    {
        private readonly IChildRepository _childRepository;
        private readonly ICurrentUser _currentUser;
        private readonly IMapper _mapper;
        private readonly ChildBusinessRules _childBusinessRules;

        public ActivateChildCommandHandler(
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

        public async Task<ChildListItem> Handle(ActivateChildCommand request, CancellationToken cancellationToken)
        {
            long userId = _currentUser.UserIdOrThrow();

            Child? child = await _childRepository.GetByIdForUserAsync(request.Id, userId, cancellationToken);
            await _childBusinessRules.ChildShouldExist(child);

            await _childRepository.DeactivateAllForUserAsync(userId, cancellationToken);
            child!.IsActive = true;
            await _childRepository.UpdateAsync(child, cancellationToken);

            return _mapper.Map<ChildListItem>(child);
        }
    }
}
