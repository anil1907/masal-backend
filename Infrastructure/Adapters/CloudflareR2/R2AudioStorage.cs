using Amazon.S3;
using Amazon.S3.Model;
using Application.Services.AudioStorage;
using Core.CrossCuttingConcerns.Exception.Types;
using Microsoft.Extensions.Options;

namespace Infrastructure.Adapters.CloudflareR2;

/// Cloudflare R2 (S3-compatible) audio store via AWSSDK.S3.
/// Endpoint: https://{AccountId}.r2.cloudflarestorage.com, region "auto", path-style.
/// Uploads MP3 as audio/mpeg into a private bucket and hands back short-lived signed GET URLs.
public class R2AudioStorage : IAudioStorage
{
    private readonly IAmazonS3 _s3;
    private readonly CloudflareR2Options _options;

    public R2AudioStorage(IAmazonS3 s3, IOptions<CloudflareR2Options> options)
    {
        _s3 = s3;
        _options = options.Value;
    }

    public async Task<string> UploadMp3Async(byte[] mp3, string objectKey, CancellationToken cancellationToken = default)
    {
        if (mp3 is null || mp3.Length == 0)
            throw new BusinessException("Yüklenecek ses verisi boş.");
        if (string.IsNullOrWhiteSpace(objectKey))
            throw new BusinessException("Nesne anahtarı (objectKey) boş olamaz.");

        using var stream = new MemoryStream(mp3, writable: false);
        var put = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = objectKey,
            InputStream = stream,
            ContentType = "audio/mpeg",
            DisablePayloadSigning = true // R2 doesn't support streaming SigV4 chunked payloads
        };

        PutObjectResponse response = await _s3.PutObjectAsync(put, cancellationToken);
        if ((int)response.HttpStatusCode >= 300)
            throw new BusinessException($"R2'ye yükleme başarısız (HTTP {(int)response.HttpStatusCode}).");

        return await GetSignedUrlAsync(objectKey, cancellationToken);
    }

    public Task<string> GetSignedUrlAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
            throw new BusinessException("Nesne anahtarı (objectKey) boş olamaz.");

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = objectKey,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddHours(_options.SignedUrlHours)
        };
        return _s3.GetPreSignedURLAsync(request);
    }
}
