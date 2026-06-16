namespace Application.Features.Stories.Commands.SynthesizeStore;

public class SynthesizeStoreResponse
{
    /// Signed GET URL (short-lived) the client streams the MP3 from.
    public string Url { get; set; } = default!;
    /// Object key the MP3 was stored under in the R2 bucket.
    public string ObjectKey { get; set; } = default!;
    /// Size of the synthesized MP3 in bytes.
    public int SizeBytes { get; set; }
}
