using Application.Features.Children.Rules;
using Application.Features.Stories.Rules;
using Application.Persistence;
using Application.Services.CurrentUser;
using Core.Application.Pipelines.Authorization;
using Core.Application.Responses;
using Domain.Entities.Children;
using Domain.Entities.Stories;
using MediatR;
using Microsoft.EntityFrameworkCore;

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
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUser _currentUser;
        private readonly ChildBusinessRules _childBusinessRules;
        private readonly StoryBusinessRules _storyBusinessRules;

        public MarkChapterListenedCommandHandler(
            IApplicationDbContext db,
            ICurrentUser currentUser,
            ChildBusinessRules childBusinessRules,
            StoryBusinessRules storyBusinessRules)
        {
            _db = db;
            _currentUser = currentUser;
            _childBusinessRules = childBusinessRules;
            _storyBusinessRules = storyBusinessRules;
        }

        public async Task<MarkChapterListenedResponse> Handle(MarkChapterListenedCommand request, CancellationToken cancellationToken)
        {
            long userId = _currentUser.UserIdOrThrow();
            Child? child = await _db.Children
                .AsNoTracking()
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.IsActive)
                .ThenByDescending(c => c.Id)
                .FirstOrDefaultAsync(cancellationToken);
            await _childBusinessRules.ChildShouldExist(child);

            StoryChapter? chapter = await _db.StoryChapters
                .FirstOrDefaultAsync(c => c.Id == request.ChapterId && c.ChildId == child!.Id, cancellationToken);
            await _storyBusinessRules.ChapterShouldExist(chapter);

            if (chapter.ListenedDate is null)
            {
                chapter.ListenedDate = DateTime.UtcNow;
                _db.StoryChapters.Update(chapter);
                await _db.SaveChangesAsync(cancellationToken);
            }

            return new MarkChapterListenedResponse { Listened = true };
        }
    }
}
