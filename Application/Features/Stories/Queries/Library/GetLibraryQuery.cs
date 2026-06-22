using Application.Features.Children.Rules;
using Application.Features.Stories.Dtos;
using Application.Services.AudioStorage;
using Application.Services.CurrentUser;
using Application.Services.Repositories;
using Core.Application.Pipelines.Authorization;
using Core.Application.Responses;
using Domain.Entities.Children;
using Domain.Entities.Stories;
using MediatR;

namespace Application.Features.Stories.Queries.Library;

public class LibraryResponse : IResponse
{
    public List<ChapterDto> Chapters { get; set; } = [];
}

/// The child's full story arc, newest chapter first. Read-only; no generation.
public class GetLibraryQuery : IRequest<LibraryResponse>, ISecuredRequest
{
    public string[] Roles => [OperationClaims.AllowAnonymous];

    public class GetLibraryQueryHandler : IRequestHandler<GetLibraryQuery, LibraryResponse>
    {
        private readonly IChildRepository _childRepository;
        private readonly IStoryChapterRepository _chapterRepository;
        private readonly ICurrentUser _currentUser;
        private readonly IAudioStorage _audio;
        private readonly ChildBusinessRules _childBusinessRules;

        public GetLibraryQueryHandler(
            IChildRepository childRepository,
            IStoryChapterRepository chapterRepository,
            ICurrentUser currentUser,
            IAudioStorage audio,
            ChildBusinessRules childBusinessRules)
        {
            _childRepository = childRepository;
            _chapterRepository = chapterRepository;
            _currentUser = currentUser;
            _audio = audio;
            _childBusinessRules = childBusinessRules;
        }

        public async Task<LibraryResponse> Handle(GetLibraryQuery request, CancellationToken cancellationToken)
        {
            long userId = _currentUser.UserIdOrThrow();
            Child? child = await _childRepository.GetActiveForUserAsync(userId, cancellationToken);
            await _childBusinessRules.ChildShouldExist(child);

            List<StoryChapter> chapters = await _chapterRepository.GetAllForChildAsync(child!.Id, cancellationToken);

            var dtos = new List<ChapterDto>(chapters.Count);
            foreach (StoryChapter c in chapters)
                dtos.Add(await ChapterDto.FromAsync(c, _audio, cancellationToken));

            return new LibraryResponse { Chapters = dtos };
        }
    }
}
