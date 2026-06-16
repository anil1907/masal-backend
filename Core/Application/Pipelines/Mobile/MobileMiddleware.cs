using Core.CrossCuttingConcerns.Exception.Types;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Core.Application.Pipelines.Mobile;

public class MobileMiddleware<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, IMobileAware
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;

    public MobileMiddleware(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            var mobileAppHeader = _httpContextAccessor.HttpContext.Request.Headers["X-Mobile-App"].FirstOrDefault();

            if (string.IsNullOrEmpty(mobileAppHeader))
                throw new AuthorizationException("You are not authorized.");

            var encryptionKey = _configuration["EncryptionKey"];

            if (mobileAppHeader != encryptionKey)
                throw new AuthorizationException("You are not authorized.");
        }
        catch
        {
            throw new AuthorizationException("You are not authorized.");
        }


        return await next();
    }
}