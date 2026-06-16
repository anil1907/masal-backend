namespace Application.Services.Tts;

/// Bound from the "GoogleTts" config section. ApiKey comes from user-secrets (dev)
/// or the host env var GoogleTts__ApiKey (prod) - never from source/appsettings.
public class GoogleTtsOptions
{
    public string ApiKey { get; set; } = "";
    public string LanguageCode { get; set; } = "tr-TR";
    /// A warm female Turkish voice. Female options: Wavenet-A, Wavenet-C, Standard-A/C/D.
    /// More natural (newer): Chirp3-HD female voices (Aoede, Kore, Leda, Zephyr).
    public string VoiceName { get; set; } = "tr-TR-Wavenet-C";
    /// Slightly slow for a calm bedtime pace (0.25-4.0; 1.0 = normal).
    public double SpeakingRate { get; set; } = 0.92;
    /// Subtle pitch nudge (-20.0 to 20.0 semitones; 0 = default).
    public double Pitch { get; set; } = 0.0;
}
