using Application.Features;
using Application.Features.Auth.Rules;
using Application.Services.AuthService;
using Application.Services.Repositories;
using Application.Services.UserService;
using AutoMapper;
using Core.Application.Dtos;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Logging;
using Core.Security.JWT;
using Domain.Entities.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Auth.Commands.Login;

public class UserLoginCommand : IRequest<UserLoggedResponse>, ISecuredRequest, ILoggableRequest
{
    public UserForLoginDto UserForLoginDto { get; set; }
    public string IpAddress { get; set; }

    public string[] Roles => [OperationClaims.AllowAnonymous];

    public class UserLoginCommandHandler : IRequestHandler<UserLoginCommand, UserLoggedResponse>
    {
        private readonly AuthBusinessRules _authBusinessRules;
        private readonly IAuthService _authService;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public UserLoginCommandHandler(
            IUserService userService,
            IAuthService authService,
            AuthBusinessRules authBusinessRules,
            IMapper mapper, IUserRepository userRepository)
        {
            _authService = authService;
            _authBusinessRules = authBusinessRules;
            _mapper = mapper;
            _userRepository = userRepository;
        }

        public async Task<UserLoggedResponse> Handle(UserLoginCommand request, CancellationToken cancellationToken)
        {
            User? user = await _userRepository.GetAsync(
                predicate: u => u.Username == request.UserForLoginDto.Username,
                cancellationToken: cancellationToken,
                include: to => to.Include(op => op.UserOperationClaims).ThenInclude(cl => cl.OperationClaim)
            );
            await _authBusinessRules.UserShouldBeExistsWhenSelected(user);
            await _authBusinessRules.UserPasswordShouldBeMatch(user!, request.UserForLoginDto.Password);

            UserLoggedResponse loggedResponse = new();

            AccessToken createdAccessToken = await _authService.CreateAccessToken(user!);

            loggedResponse.AccessToken = createdAccessToken;
            loggedResponse.Claims = user!.UserOperationClaims.Select(uoc => uoc.OperationClaim.Name).ToList();
            return loggedResponse;
        }
    }
}
