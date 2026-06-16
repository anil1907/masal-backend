namespace Application.Services.Tts;

/// Text-to-speech: turns an (already safety-passed) story chapter into narrated MP3 bytes.
/// Provider-agnostic; the live implementation is Google Cloud TTS.
public interface ITtsSynthesizer
{
    Task<byte[]> SynthesizeMp3Async(string text, CancellationToken cancellationToken = default);
}
