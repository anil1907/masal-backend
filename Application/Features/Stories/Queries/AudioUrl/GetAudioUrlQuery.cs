using Application.Services.AudioStorage;
using Core.Application.Pipelines.Authorization;
using Core.Application.Responses;
using MediatR;

namespace Application.Features.Stories.Queries.AudioUrl;

public class AudioUrlResponse : IResponse
{
    public string Url { get; set; } = "";
}

/// Re-mint a fresh short-lived signed GET URL for an already-stored chapter MP3,
/// so a client can re-open a chapter without regenerating it.
public class GetAudioUrlQuery : IRequest<AudioUrlResponse>, ISecuredRequest
{
    public string ObjectKey { get; set; } = default!;

    public string[] Roles => [OperationClaims.AllowAnonymous];

    public class GetAudioUrlQueryHandler : IRequestHandler<GetAudioUrlQuery, AudioUrlResponse>
    {
        private readonly IAudioStorage _audio;

        public GetAudioUrlQueryHandler(IAudioStorage audio) => _audio = audio;

        public async Task<AudioUrlResponse> Handle(GetAudioUrlQuery request, CancellationToken cancellationToken)
        {
            string url = await _audio.GetSignedUrlAsync(request.ObjectKey, cancellationToken);
            return new AudioUrlResponse { Url = url };
        }
    }
}
