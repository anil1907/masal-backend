using Core.Repositories;
using Domain.Entities.Users;

namespace Domain.Entities.Stories;

/// One row per LLM story generation - drives the per-user daily quota
/// (and doubles as cost telemetry). CreatedDate comes from the Entity base.
public class StoryGenerationLog : Entity
{
    public long UserId { get; set; }
    public virtual User User { get; set; } = default!;
}
