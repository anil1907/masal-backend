using Application.Features.Stories.Dtos;
using Core.Application.Responses;

namespace Application.Features.Stories.Commands.Tonight;

/// What to show on the home screen tonight.
/// - status "ready"            => `Chapter` is playable now (Available = true).
/// - status "comeBackTomorrow" => tonight's chapter was already heard; `Chapter` is that last
///                                chapter (for context) and Available = false. The next one is
///                                generated on the next day (1 story/day).
/// - status "freeLimitReached" => free-tier weekly allowance is used up; `Chapter` is the last
///                                chapter (re-listenable) and Available = false. Prompt the paywall.
public class TonightStoryResponse : IResponse
{
    public const string StatusReady = "ready";
    public const string StatusComeBackTomorrow = "comeBackTomorrow";
    public const string StatusFreeLimitReached = "freeLimitReached";

    public string Status { get; set; } = StatusReady;
    public bool Available { get; set; }
    public ChapterDto? Chapter { get; set; }
}
