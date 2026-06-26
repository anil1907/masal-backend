using Application.Features.Children.Rules;
using Application.Features.Stories.Dtos;
using Application.Persistence;
using Application.Services.AudioStorage;
using Application.Services.CurrentUser;
using Core.Application.Pipelines.Authorization;
using Core.Application.Responses;
using Domain.Entities.Children;
using Domain.Entities.Stories;
using MediatR;
using Microsoft.EntityFrameworkCore;

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
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUser _currentUser;
        private readonly IAudioStorage _audio;
        private readonly ChildBusinessRules _childBusinessRules;

        public GetLibraryQueryHandler(
            IApplicationDbContext db,
            ICurrentUser currentUser,
            IAudioStorage audio,
            ChildBusinessRules childBusinessRules)
        {
            _db = db;
            _currentUser = currentUser;
            _audio = audio;
            _childBusinessRules = childBusinessRules;
        }

        public async Task<LibraryResponse> Handle(GetLibraryQuery request, CancellationToken cancellationToken)
        {
            long userId = _currentUser.UserIdOrThrow();
            Child? child = await _db.Children
                .AsNoTracking()
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.IsActive)
                .ThenByDescending(c => c.Id)
                .FirstOrDefaultAsync(cancellationToken);
            await _childBusinessRules.ChildShouldExist(child);

            List<StoryChapter> chapters = await _db.StoryChapters
                .AsNoTracking()
                .Where(c => c.ChildId == child!.Id)
                .OrderByDescending(c => c.Number)
                .ToListAsync(cancellationToken);

            var dtos = new List<ChapterDto>(chapters.Count);
            foreach (StoryChapter c in chapters)
                dtos.Add(await ChapterDto.FromAsync(c, _audio, cancellationToken));

            return new LibraryResponse { Chapters = dtos };
        }
    }
}
