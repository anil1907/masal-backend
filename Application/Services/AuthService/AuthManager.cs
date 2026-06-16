using Application.Services.Repositories;
using Application.Services.Token;
using AutoMapper;
using Core.Security.JWT;
using Domain.Entities.Users;
using Microsoft.Extensions.Options;

namespace Application.Services.AuthService;

public class AuthManager : IAuthService
{
    private readonly TokenOptions _tokenOptions;
    private readonly IMapper _mapper;
    private readonly IUserOperationClaimRepository _operationClaimRepository;
    private readonly ITokenHelper _tokenHelper;

    public AuthManager(
        IOptions<TokenOptions> tokenOptions,
        IMapper mapper,
        IUserOperationClaimRepository operationClaimRepository,
        ITokenHelper tokenHelper)
    {
        _tokenOptions = tokenOptions.Value;
        _mapper = mapper;
        _operationClaimRepository = operationClaimRepository;
        _tokenHelper = tokenHelper;
    }

    public async Task<AccessToken> CreateAccessToken(User user)
    {
        IList<OperationClaim> operationClaims =
            await _operationClaimRepository.GetOperationClaimsByUserIdAsync(user.Id);
        AccessToken accessToken = _tokenHelper.CreateToken(
            user,
            operationClaims.ToList()
        );
        return accessToken;
    }
}
