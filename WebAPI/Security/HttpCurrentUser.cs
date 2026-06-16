using Application.Services.CurrentUser;
using Core.CrossCuttingConcerns.Exception.Types;
using Core.Security.Extensions;

namespace WebAPI.Security;

/// Reads the authenticated user's long id from the JWT nameidentifier claim.
public class HttpCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public long? UserId
    {
        get
        {
            string? idClaim = _httpContextAccessor.HttpContext?.User.GetIdClaim();
            return long.TryParse(idClaim, out long id) ? id : null;
        }
    }

    public long UserIdOrThrow()
        => UserId ?? throw new AuthorizationException("Oturum bulunamadı.");
}
