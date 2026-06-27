namespace Application.Features.Stories.Dtos;

/// A story series (named arc) with its chapters nested - the library's single payload.
public class SeriesDto
{
    public long Id { get; set; }
    public string Title { get; set; } = "";
    public int ChapterCount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ChapterDto> Chapters { get; set; } = [];
}
