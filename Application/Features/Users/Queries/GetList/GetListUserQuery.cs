using Application.Features.Users.Constants;
using Application.Services.Repositories;
using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Logging;
using Core.Application.Requests;
using Core.Application.Responses;
using Core.Persistence.Paging;
using Domain.Entities.Users;
using MediatR;

namespace Application.Features.Users.Queries.GetList;

public class GetListUserQuery : IRequest<GetListResponse<GetListUserListItemDto>>, ISecuredRequest, ILoggableRequest
{
    public PageRequest PageRequest { get; set; } = new() { PageIndex = 0, PageSize = 10 };

    public string[] Roles => [OperationClaims.GeneralAdmin, UserOperationClaims.List];

    public class GetListUserQueryHandler : IRequestHandler<GetListUserQuery, GetListResponse<GetListUserListItemDto>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public GetListUserQueryHandler(IUserRepository userRepository, IMapper mapper)
        {
            _userRepository = userRepository;
            _mapper = mapper;
        }

        public async Task<GetListResponse<GetListUserListItemDto>> Handle(GetListUserQuery request,
            CancellationToken cancellationToken)
        {
            IPaginate<User> users = await _userRepository.GetListAsync(
                index: request.PageRequest.PageIndex,
                size: request.PageRequest.PageSize,
                cancellationToken: cancellationToken);

            GetListResponse<GetListUserListItemDto> response =
                _mapper.Map<GetListResponse<GetListUserListItemDto>>(users);
            return response;
        }
    }
}
