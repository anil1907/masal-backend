namespace Application.Services.Push;

/// Bound from the "Apns" config section (set via Render env: Apns__KeyId, Apns__TeamId, ...).
public class ApnsOptions
{
    /// The 10-char Key ID of the APNs auth key (.p8).
    public string KeyId { get; set; } = "";
    /// The 10-char Apple Developer Team ID.
    public string TeamId { get; set; } = "";
    /// The app bundle id == apns-topic.
    public string BundleId { get; set; } = "com.anilyildirim.masal";
    /// PEM contents of the .p8 auth key (the whole "-----BEGIN PRIVATE KEY----- ..." block).
    public string PrivateKey { get; set; } = "";

    /// True only when all required secrets are present; otherwise the sender no-ops.
    public bool Enabled =>
        !string.IsNullOrWhiteSpace(KeyId)
        && !string.IsNullOrWhiteSpace(TeamId)
        && !string.IsNullOrWhiteSpace(PrivateKey);
}
