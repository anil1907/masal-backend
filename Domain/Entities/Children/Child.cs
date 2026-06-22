using Core.Repositories;
using Domain.Entities.Users;

namespace Domain.Entities.Children;

/// A child ("hero"). A parent can have several (free: 1, premium: up to 3). Exactly one is
/// active at a time - story generation always targets the active child. Fears are AVOIDED in
/// stories, interests are woven in. Lists are stored as Postgres text[] (Npgsql native).
public class Child : Entity
{
    public long UserId { get; set; }
    public virtual User User { get; set; } = default!;

    public string HeroName { get; set; } = default!;
    public List<string> Fears { get; set; } = [];
    public List<string> Interests { get; set; } = [];
    public string? AgeBand { get; set; }
    /// "kız" | "erkek" | null. Guides pronouns / how the hero is portrayed.
    public string? Gender { get; set; }
    /// The currently selected child for this parent. Exactly one true per user.
    public bool IsActive { get; set; }
}
