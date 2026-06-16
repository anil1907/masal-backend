namespace Application.Services.CurrentUser;

/// Reads the authenticated user's id from the request (JWT nameidentifier claim).
/// Our user ids are long (NOT Guid - the BaseController helper does not fit).
public interface ICurrentUser
{
    long? UserId { get; }
    long UserIdOrThrow();
}
