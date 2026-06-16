using Application.Features.Children.Rules;
using Application.Features.Stories.Rules;
using Application.Services.CurrentUser;
using Application.Services.Repositories;
using Core.Application.Pipelines.Authorization;
using Core.Application.Responses;
using Domain.Entities.Children;
using Domain.Entities.Stories;
using MediatR;

namespace Application.Features.Stories.Commands.MarkListened;

public class MarkChapterListenedResponse : IResponse
{
    public bool Listened { get; set; }
}

/// Mark a chapter as fully heard. Idempotent: only stamps ListenedDate the first time.
/// This is the signal that unlocks the next day's chapter (1 story/day).
public class MarkChapterListenedCommand : IRequest<MarkChapterListenedResponse>, ISecuredRequest
{
    public long ChapterId { get; set; }

    public string[] Roles => [OperationClaims.AllowAnonymous];

    public class MarkChapterListenedCommandHandler : IRequestHandler<MarkChapterListenedCommand, MarkChapterListenedResponse>
    {
        private readonly IChildRepository _childRepository;
        private readonly IStoryChapterRepository _chapterRepository;
        private readonly ICurrentUser _currentUser;
        private readonly ChildBusinessRules _childBusinessRules;
        private readonly StoryBusinessRules _storyBusinessRules;

        public MarkChapterListenedCommandHandler(
            IChildRepository childRepository,
            IStoryChapterRepository chapterRepository,
            ICurrentUser currentUser,
            ChildBusinessRules childBusinessRules,
            StoryBusinessRules storyBusinessRules)
        {
            _childRepository = childRepository;
            _chapterRepository = chapterRepository;
            _currentUser = currentUser;
            _childBusinessRules = childBusinessRules;
            _storyBusinessRules = storyBusinessRules;
        }

        public async Task<MarkChapterListenedResponse> Handle(MarkChapterListenedCommand request, CancellationToken cancellationToken)
        {
            long userId = _currentUser.UserIdOrThrow();
            Child? child = await _childRepository.GetByUserIdAsync(userId, cancellationToken);
            await _childBusinessRules.ChildShouldExist(child);

            StoryChapter? chapter = await _chapterRepository.GetForChildAsync(request.ChapterId, child!.Id, cancellationToken);
            await _storyBusinessRules.ChapterShouldExist(chapter);

            if (chapter.ListenedDate is null)
            {
                chapter.ListenedDate = DateTime.UtcNow;
                await _chapterRepository.UpdateAsync(chapter, cancellationToken);
            }

            return new MarkChapterListenedResponse { Listened = true };
        }
    }
}
