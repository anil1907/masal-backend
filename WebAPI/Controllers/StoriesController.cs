using Application.Features.Stories.Commands.ActivateSeries;
using Application.Features.Stories.Commands.Generate;
using Application.Features.Stories.Commands.GenerateChapter;
using Application.Features.Stories.Commands.MarkListened;
using Application.Features.Stories.Commands.NewSeries;
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

    /// Read-only home state for the active series (latest chapter + whether a story can be generated).
    [HttpPost("tonight")]
    public async Task<IActionResult> Tonight()
        => Ok(await Mediator.Send(new GetTonightStoryCommand()));

    /// Explicit "create tonight's story" action (the button). Enqueues generation (1/day); the client
    /// polls `tonight` until the new chapter appears.
    [HttpPost("generate")]
    public async Task<IActionResult> Generate()
        => Ok(await Mediator.Send(new GenerateStoryCommand()));

    /// The child's full story arc (library), newest first. Read-only.
    [HttpGet("library")]
    public async Task<IActionResult> Library()
        => Ok(await Mediator.Send(new GetLibraryQuery()));

    /// Start a brand-new story series (the current one stays resumable). Returns "preparing".
    [HttpPost("series/new")]
    public async Task<IActionResult> NewSeries()
        => Ok(await Mediator.Send(new NewStorySeriesCommand()));

    /// Resume a paused series: make it active so tonight continues it.
    [HttpPost("series/activate")]
    public async Task<IActionResult> ActivateSeries([FromBody] ActivateSeriesCommand command)
        => Ok(await Mediator.Send(command));

    /// Mark a chapter as fully heard - unlocks the next day's chapter.
    [HttpPost("mark-listened")]
    public async Task<IActionResult> MarkListened([FromBody] MarkChapterListenedCommand command)
        => Ok(await Mediator.Send(command));

    /// Re-mint a fresh signed audio URL for an already-stored chapter (re-open without regenerating).
    [HttpGet("audio-url")]
    public async Task<IActionResult> AudioUrl([FromQuery] GetAudioUrlQuery query)
        => Ok(await Mediator.Send(query));
}

