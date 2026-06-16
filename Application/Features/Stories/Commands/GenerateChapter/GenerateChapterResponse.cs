using Core.Application.Responses;

namespace Application.Features.Stories.Commands.GenerateChapter;

public class GenerateChapterResponse : IResponse
{
    public string Title { get; set; } = "";
    public string Text { get; set; } = "";
    public string Summary { get; set; } = "";
    public bool SafetyPassed { get; set; }
    public string SafetyReason { get; set; } = "";
}
