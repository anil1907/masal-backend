using Core.Repositories;

namespace Domain.Entities.Auth;

public class PhoneOtp : Entity
{
    public string PhoneNumber { get; set; } = default!;
    public byte[] CodeHash { get; set; } = default!;
    public byte[] CodeSalt { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
    public int AttemptCount { get; set; }
    public bool IsUsed { get; set; }
}
