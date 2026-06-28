using Core.Repositories;
using Domain.Entities.Children;

namespace Domain.Entities.Stories;

/// One persisted, narrated chapter in a child's ongoing bedtime-story arc.
/// Continuity: each chapter stores a running Summary used as the prompt context for the next.
/// Cost control: a chapter is generated + synthesized once and reused.
public class StoryChapter : Entity
{
    public long ChildId { get; set; }
    public virtual Child Child { get; set; } = default!;

    /// The series (named arc) this chapter belongs to.
    public long SeriesId { get; set; }
    public virtual StorySeries Series { get; set; } = default!;

    /// 1-based position within its series.
    public int Number { get; set; }
    public string Title { get; set; } = default!;
    public string Text { get; set; } = default!;
    /// Short running summary to resume the arc from on the next chapter.
    public string Summary { get; set; } = default!;

    /// R2 object key for the narrated MP3 (signed URLs are minted on demand).
    public string AudioObjectKey { get; set; } = default!;
    public int DurationSeconds { get; set; }
}
