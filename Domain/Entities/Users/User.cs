using Core.Repositories;

namespace Domain.Entities.Users;

public class User : Entity
{
    public string Username { get; set; }
    public string Email { get; set; }
    public string? PhoneNumber { get; set; }
    /// Stable Apple user identifier (the `sub` claim from Sign in with Apple). null for phone users.
    public string? AppleUserId { get; set; }
    public byte[] PasswordSalt { get; set; }
    public byte[] PasswordHash { get; set; }

    public virtual ICollection<UserOperationClaim> UserOperationClaims { get; set; } = default!;
}
