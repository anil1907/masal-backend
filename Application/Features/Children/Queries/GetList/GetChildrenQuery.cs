using Application.Services.CurrentUser;
using Application.Services.Repositories;
using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Logging;
using Domain.Entities.Children;
using MediatR;

namespace Application.Features.Children.Queries.GetList;

public class GetChildrenQuery : IRequest<GetChildrenResponse>, ISecuredRequest, ILoggableRequest
{
    public string[] Roles => [OperationClaims.AllowAnonymous];

    public class GetChildrenQueryHandler : IRequestHandler<GetChildrenQuery, GetChildrenResponse>
    {
        private readonly IChildRepository _childRepository;
        private readonly ICurrentUser _currentUser;
        private readonly IMapper _mapper;

        public GetChildrenQueryHandler(IChildRepository childRepository, ICurrentUser currentUser, IMapper mapper)
        {
            _childRepository = childRepository;
            _currentUser = currentUser;
            _mapper = mapper;
        }

        public async Task<GetChildrenResponse> Handle(GetChildrenQuery request, CancellationToken cancellationToken)
        {
            long userId = _currentUser.UserIdOrThrow();
            List<Child> children = await _childRepository.GetAllForUserAsync(userId, cancellationToken);
            return new GetChildrenResponse
            {
                Children = children.Select(_mapper.Map<ChildListItem>).ToList()
            };
        }
    }
}
