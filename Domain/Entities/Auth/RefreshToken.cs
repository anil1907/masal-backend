using Core.Repositories;
using Domain.Entities.Users;

namespace Domain.Entities.Auth;

/// Long-lived session token. The RAW value is returned to the client once and never stored;
/// we keep only the SHA-256 hash. Rotated on every use (old row revoked, new row issued).
public class RefreshToken : Entity
{
    public long UserId { get; set; }
    public virtual User User { get; set; } = default!;

    public string TokenHash { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}
