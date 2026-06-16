using Application.Services.Tts;
using Core.Application.Pipelines.Authorization;
using MediatR;

namespace Application.Features.Stories.Commands.SynthesizePreview;

/// Dev/preview: synthesize arbitrary text to MP3 to audition the Google TTS voice.
/// Returns raw bytes (no ILoggableRequest - we don't want MP3 bytes in the logs).
public class SynthesizePreviewCommand : IRequest<byte[]>, ISecuredRequest
{
    public string Text { get; set; } = default!;

    public string[] Roles => [OperationClaims.AllowAnonymous];

    public class SynthesizePreviewCommandHandler : IRequestHandler<SynthesizePreviewCommand, byte[]>
    {
        private readonly ITtsSynthesizer _tts;

        public SynthesizePreviewCommandHandler(ITtsSynthesizer tts) => _tts = tts;

        public Task<byte[]> Handle(SynthesizePreviewCommand request, CancellationToken cancellationToken)
            => _tts.SynthesizeMp3Async(request.Text, cancellationToken);
    }
}
