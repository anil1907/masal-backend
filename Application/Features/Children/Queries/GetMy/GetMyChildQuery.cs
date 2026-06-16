using Application.Features.Children.Rules;
using Application.Services.CurrentUser;
using Application.Services.Repositories;
using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Logging;
using Domain.Entities.Children;
using MediatR;

namespace Application.Features.Children.Queries.GetMy;

public class GetMyChildQuery : IRequest<GetMyChildResponse>, ISecuredRequest, ILoggableRequest
{
    public string[] Roles => [OperationClaims.AllowAnonymous];

    public class GetMyChildQueryHandler : IRequestHandler<GetMyChildQuery, GetMyChildResponse>
    {
        private readonly IChildRepository _childRepository;
        private readonly ICurrentUser _currentUser;
        private readonly IMapper _mapper;
        private readonly ChildBusinessRules _childBusinessRules;

        public GetMyChildQueryHandler(
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

        public async Task<GetMyChildResponse> Handle(GetMyChildQuery request, CancellationToken cancellationToken)
        {
            long userId = _currentUser.UserIdOrThrow();
            Child? child = await _childRepository.GetByUserIdAsync(userId, cancellationToken);
            await _childBusinessRules.ChildShouldExist(child);
            return _mapper.Map<GetMyChildResponse>(child);
        }
    }
}
