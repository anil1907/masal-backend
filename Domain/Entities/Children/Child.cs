using Core.Repositories;
using Domain.Entities.Users;

namespace Domain.Entities.Children;

/// One child ("hero") per parent account in the MVP. Fears are AVOIDED in stories,
/// interests are woven in. Lists are stored as Postgres text[] (Npgsql native).
public class Child : Entity
{
    public long UserId { get; set; }
    public virtual User User { get; set; } = default!;

    public string HeroName { get; set; } = default!;
    public List<string> Fears { get; set; } = [];
    public List<string> Interests { get; set; } = [];
    public string? AgeBand { get; set; }
}
