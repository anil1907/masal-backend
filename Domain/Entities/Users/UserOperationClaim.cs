using Core.Repositories;

namespace Domain.Entities.Users;

public class UserOperationClaim : Entity
{
    public long UserId { get; set; }
    public virtual User User { get; set; } = default!;
    public long OperationClaimId { get; set; }
    public virtual OperationClaim OperationClaim { get; set; } = default!;
}
