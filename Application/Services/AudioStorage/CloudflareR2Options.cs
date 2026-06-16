namespace Application.Services.AudioStorage;

/// Bound from the "CloudflareR2" config section. The three credentials come from
/// user-secrets (dev) or host env vars CloudflareR2__* (prod) - never from source/appsettings.
/// BucketName/SignedUrlHours are safe to keep in appsettings.
public class CloudflareR2Options
{
    /// Cloudflare account id; forms the S3 endpoint https://{AccountId}.r2.cloudflarestorage.com.
    public string AccountId { get; set; } = "";
    public string AccessKeyId { get; set; } = "";
    public string SecretAccessKey { get; set; } = "";
    /// R2 bucket that holds the narrated MP3s.
    public string BucketName { get; set; } = "masal-audio";
    /// Lifetime of issued signed GET URLs, in hours.
    public int SignedUrlHours { get; set; } = 12;

    /// S3 endpoint derived from the account id.
    public string ServiceUrl => $"https://{AccountId}.r2.cloudflarestorage.com";
}
