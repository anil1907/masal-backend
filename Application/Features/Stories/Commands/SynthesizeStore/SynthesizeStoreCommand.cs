using Application.Services.AudioStorage;
using Application.Services.Tts;
using Core.Application.Pipelines.Authorization;
using MediatR;

namespace Application.Features.Stories.Commands.SynthesizeStore;

/// Dev/validation: text -> Google TTS MP3 -> upload to Cloudflare R2 -> signed GET URL.
/// Proves the full narration-storage pipeline end to end.
public class SynthesizeStoreCommand : IRequest<SynthesizeStoreResponse>, ISecuredRequest
{
    public string Text { get; set; } = default!;

    public string[] Roles => [OperationClaims.AllowAnonymous];

    public class SynthesizeStoreCommandHandler : IRequestHandler<SynthesizeStoreCommand, SynthesizeStoreResponse>
    {
        private readonly ITtsSynthesizer _tts;
        private readonly IAudioStorage _audio;

        public SynthesizeStoreCommandHandler(ITtsSynthesizer tts, IAudioStorage audio)
        {
            _tts = tts;
            _audio = audio;
        }

        public async Task<SynthesizeStoreResponse> Handle(SynthesizeStoreCommand request, CancellationToken cancellationToken)
        {
            byte[] mp3 = await _tts.SynthesizeMp3Async(request.Text, cancellationToken);
            string objectKey = $"previews/{Guid.NewGuid():N}.mp3";
            string url = await _audio.UploadMp3Async(mp3, objectKey, cancellationToken);

            return new SynthesizeStoreResponse
            {
                Url = url,
                ObjectKey = objectKey,
                SizeBytes = mp3.Length
            };
        }
    }
}
