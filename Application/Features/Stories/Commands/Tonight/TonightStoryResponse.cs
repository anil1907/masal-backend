using Application.Features.Stories.Dtos;
using Core.Application.Responses;

namespace Application.Features.Stories.Commands.Tonight;

/// Home-screen state for the child's active series. Read-only: generation is an explicit user action
/// (POST /api/Stories/generate), capped at 1 per day.
/// - "ready"     => `Chapter` is the latest playable chapter.
/// - "empty"     => no chapter yet; show the "create story" prompt.
/// - "preparing" => a generation is running; show the waiting screen and poll.
/// - "failed"    => last generation failed; the user can generate again.
/// `CanGenerate` drives the "create story" button; `BlockedReason` explains why it is disabled.
public class TonightStoryResponse : IResponse
{
    public const string StatusReady = "ready";
    public const string StatusEmpty = "empty";
    public const string StatusPreparing = "preparing";
    public const string StatusFailed = "failed";

    // Why generation is unavailable (when CanGenerate is false and not preparing).
    public const string BlockedToday = "today";          // already generated today's story
    public const string BlockedFreeLimit = "freeLimit";  // free weekly allowance used up

    public string Status { get; set; } = StatusEmpty;
    public ChapterDto? Chapter { get; set; }
    public bool CanGenerate { get; set; }
    public string? BlockedReason { get; set; }
    public string? SeriesTitle { get; set; }
}
