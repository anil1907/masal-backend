using Amazon.Runtime;
using Amazon.S3;
using Application.Services.AppleAuth;
using Application.Services.AudioStorage;
using Application.Services.SmsService;
using Application.Services.Store;
using Application.Services.StoryGeneration;
using Application.Services.Tts;
using Infrastructure.Adapters.Anthropic;
using Infrastructure.Adapters.AppleAuth;
using Infrastructure.Adapters.CloudflareR2;
using Infrastructure.Adapters.GoogleTts;
using Infrastructure.Adapters.SmsService;
using Infrastructure.Adapters.Store;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // SMS transport. Dev/test uses the console sender (no cost, no real SMS).
        // For production, swap this single line to a Netgsm adapter implementing ISmsSender.
        services.AddScoped<ISmsSender, ConsoleSmsSender>();

        // Story generation via Anthropic (Claude). The safety gate is a separate model call.
        services.AddHttpClient<AnthropicClient>(c =>
        {
            c.BaseAddress = new Uri("https://api.anthropic.com");
            c.Timeout = TimeSpan.FromSeconds(60);
        });
        services.AddScoped<IStoryGenerator, AnthropicStoryGenerator>();
        services.AddScoped<IStorySafetyGate, AnthropicSafetyGate>();

        // Narration via Google Cloud Text-to-Speech (REST + API key).
        services.AddHttpClient<ITtsSynthesizer, GoogleTtsSynthesizer>(c =>
        {
            c.BaseAddress = new Uri("https://texttospeech.googleapis.com");
            c.Timeout = TimeSpan.FromSeconds(60);
        });

        // Narrated-MP3 object store on Cloudflare R2 (S3-compatible). The S3 client is a
        // singleton built from CloudflareR2 options (credentials in user-secrets/env).
        services.AddSingleton<IAmazonS3>(sp =>
        {
            CloudflareR2Options r2 = sp.GetRequiredService<IOptions<CloudflareR2Options>>().Value;
            var config = new AmazonS3Config
            {
                ServiceURL = r2.ServiceUrl,
                ForcePathStyle = true,           // R2 expects path-style addressing
                AuthenticationRegion = "auto"    // R2 region token
            };
            var creds = new BasicAWSCredentials(r2.AccessKeyId, r2.SecretAccessKey);
            return new AmazonS3Client(creds, config);
        });
        services.AddScoped<IAudioStorage, R2AudioStorage>();

        // Store purchase verification: validates the StoreKit 2 signed transaction (JWS) against Apple's root CA.
        services.AddScoped<IStoreVerifier, AppleStoreVerifier>();

        // Sign in with Apple: validates identity tokens against Apple's public JWKS.
        services.AddHttpClient<IAppleAuthVerifier, AppleAuthVerifier>(c =>
        {
            c.BaseAddress = new Uri("https://appleid.apple.com");
            c.Timeout = TimeSpan.FromSeconds(15);
        });
        return services;
    }
}
