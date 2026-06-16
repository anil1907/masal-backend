using Application.Services.AudioStorage;
using Domain.Entities.Stories;

namespace Application.Features.Stories.Dtos;

/// A chapter as the client needs it to list + play. No story Text (the app only plays audio);
/// AudioUrl is a freshly minted short-lived signed GET URL.
public class ChapterDto
{
    public long Id { get; set; }
    public int Number { get; set; }
    public string Title { get; set; } = "";
    public string Summary { get; set; } = "";
    public string AudioUrl { get; set; } = "";
    public string AudioObjectKey { get; set; } = "";
    public int DurationSeconds { get; set; }
    public bool Listened { get; set; }
    public DateTime CreatedAt { get; set; }

    public static async Task<ChapterDto> FromAsync(StoryChapter c, IAudioStorage audio, CancellationToken ct)
        => new()
        {
            Id = c.Id,
            Number = c.Number,
            Title = c.Title,
            Summary = c.Summary,
            AudioUrl = await audio.GetSignedUrlAsync(c.AudioObjectKey, ct),
            AudioObjectKey = c.AudioObjectKey,
            DurationSeconds = c.DurationSeconds,
            Listened = c.ListenedDate.HasValue,
            CreatedAt = c.CreatedDate
        };
}
