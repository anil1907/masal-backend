using Application.Features.Stories.Commands.GenerateChapter;
using Application.Features.Stories.Commands.MarkListened;
using Application.Features.Stories.Commands.SynthesizePreview;
using Application.Features.Stories.Commands.SynthesizeStore;
using Application.Features.Stories.Commands.Tonight;
using Application.Features.Stories.Queries.AudioUrl;
using Application.Features.Stories.Queries.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class StoriesController : BaseController
{
    /// Dev/preview: generate tonight's chapter for the caller's child + run the safety gate.
    [HttpPost("generate-preview")]
    public async Task<IActionResult> GeneratePreview([FromBody] GenerateChapterCommand command)
        => Ok(await Mediator.Send(command));

    /// Dev/preview: synthesize text to MP3 to audition the Google TTS voice.
    [HttpPost("synthesize-preview")]
    public async Task<IActionResult> SynthesizePreview([FromBody] SynthesizePreviewCommand command)
    {
        byte[] mp3 = await Mediator.Send(command);
        return File(mp3, "audio/mpeg", "preview.mp3");
    }

    /// Dev/validation: text -> TTS -> upload to Cloudflare R2 -> signed GET URL.
    [HttpPost("synthesize-store")]
    public async Task<IActionResult> SynthesizeStore([FromBody] SynthesizeStoreCommand command)
        => Ok(await Mediator.Send(command));

    /// Home screen state: tonight's playable chapter, or "come back tomorrow" (1 story/day).
    /// Generates only when needed (first story / new day after the previous was heard).
    [HttpPost("tonight")]
    public async Task<IActionResult> Tonight()
        => Ok(await Mediator.Send(new GetTonightStoryCommand()));

    /// The child's full story arc (library), newest first. Read-only.
    [HttpGet("library")]
    public async Task<IActionResult> Library()
        => Ok(await Mediator.Send(new GetLibraryQuery()));

    /// Mark a chapter as fully heard - unlocks the next day's chapter.
    [HttpPost("mark-listened")]
    public async Task<IActionResult> MarkListened([FromBody] MarkChapterListenedCommand command)
        => Ok(await Mediator.Send(command));

    /// Re-mint a fresh signed audio URL for an already-stored chapter (re-open without regenerating).
    [HttpGet("audio-url")]
    public async Task<IActionResult> AudioUrl([FromQuery] GetAudioUrlQuery query)
        => Ok(await Mediator.Send(query));
}

