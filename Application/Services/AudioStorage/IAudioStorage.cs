namespace Application.Services.AudioStorage;

/// Object storage for narrated story MP3s. The live implementation is Cloudflare R2
/// (S3-compatible). Audio is uploaded as audio/mpeg and served via short-lived,
/// signed GET URLs so the bucket itself stays private.
public interface IAudioStorage
{
    /// Upload MP3 bytes under <paramref name="objectKey"/> and return a signed GET URL
    /// the client can stream from. The URL expires after the configured window (default 12h).
    Task<string> UploadMp3Async(byte[] mp3, string objectKey, CancellationToken cancellationToken = default);

    /// Re-issue a signed GET URL for an already-stored object (e.g. re-opening a saved chapter)
    /// without re-uploading. Does not verify existence.
    Task<string> GetSignedUrlAsync(string objectKey, CancellationToken cancellationToken = default);
}
