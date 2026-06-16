using System.Security.Authentication;
using Core.CrossCuttingConcerns.Exception.Types;
using Core.Security.Extensions;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Core.Application.Pipelines.Authorization;

public class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>, ISecuredRequest
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserAuthorizationService _userAuthorizationService;

    public AuthorizationBehavior(
        IHttpContextAccessor httpContextAccessor,
        IUserAuthorizationService userAuthorizationService)
    {
        _httpContextAccessor = httpContextAccessor;
        _userAuthorizationService = userAuthorizationService;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var userId = _httpContextAccessor.HttpContext?.User?.GetIdClaim();
        
        if (string.IsNullOrEmpty(userId))
            throw new AuthenticationException("You are not authenticated.");

        bool isAuthorized = await _userAuthorizationService.HasAnyRoleAsync(userId, request.Roles);
        
        if (!isAuthorized)
            throw new AuthorizationException("You are not authorized.");

        return await next();
    }
}