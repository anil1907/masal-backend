using Core.Repositories;
using Domain.Entities.Children;

namespace Domain.Entities.Stories;

/// A named story arc for a child. A child can have several series (e.g. when they get bored and
/// start a fresh story) and resume any of them. Exactly one series per child is "active" - that's
/// the one tonight continues. Title is set from the first chapter's generated title.
public class StorySeries : Entity
{
    public long ChildId { get; set; }
    public virtual Child Child { get; set; } = default!;

    public string Title { get; set; } = "Yeni masal";
    public bool IsActive { get; set; }
}
