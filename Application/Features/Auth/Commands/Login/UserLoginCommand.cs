using Application.Features;
using Application.Features.Auth.Rules;
using Application.Persistence;
using Application.Services.Token;
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
        private readonly ITokenHelper _tokenHelper;
        private readonly IApplicationDbContext _db;

        public UserLoginCommandHandler(
            ITokenHelper tokenHelper,
            AuthBusinessRules authBusinessRules,
            IApplicationDbContext db)
        {
            _tokenHelper = tokenHelper;
            _authBusinessRules = authBusinessRules;
            _db = db;
        }

        public async Task<UserLoggedResponse> Handle(UserLoginCommand request, CancellationToken cancellationToken)
        {
            User? user = await _db.Users
                .AsSplitQuery()
                .Include(op => op.UserOperationClaims).ThenInclude(cl => cl.OperationClaim)
                .FirstOrDefaultAsync(u => u.Username == request.UserForLoginDto.Username, cancellationToken);
            await _authBusinessRules.UserShouldBeExistsWhenSelected(user);
            await _authBusinessRules.UserPasswordShouldBeMatch(user!, request.UserForLoginDto.Password);

            UserLoggedResponse loggedResponse = new();

            // Build the access token from the already-loaded operation claims.
            List<OperationClaim> operationClaims = user!.UserOperationClaims
                .Select(uoc => new OperationClaim { Id = uoc.OperationClaimId, Name = uoc.OperationClaim.Name })
                .ToList();
            loggedResponse.AccessToken = _tokenHelper.CreateToken(user!, operationClaims);
            loggedResponse.Claims = user!.UserOperationClaims.Select(uoc => uoc.OperationClaim.Name).ToList();
            return loggedResponse;
        }
    }
}
