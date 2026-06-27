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
    // Series (newest first), each with its chapters nested (newest chapter first).
    public List<SeriesDto> Series { get; set; } = [];
}

/// The child's full library: series with their chapters nested, in one payload. Read-only.
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

            List<StorySeries> series = await _db.StorySeries
                .AsNoTracking()
                .Where(s => s.ChildId == child!.Id)
                .OrderByDescending(s => s.Id)
                .ToListAsync(cancellationToken);

            List<StoryChapter> chapters = await _db.StoryChapters
                .AsNoTracking()
                .Where(c => c.ChildId == child!.Id)
                .OrderByDescending(c => c.Number)   // newest chapter first within each series
                .ToListAsync(cancellationToken);

            // Map each chapter once (signs its audio URL), bucketed by series.
            var chaptersBySeries = new Dictionary<long, List<ChapterDto>>();
            foreach (StoryChapter c in chapters)
            {
                if (!chaptersBySeries.TryGetValue(c.SeriesId, out List<ChapterDto>? bucket))
                    chaptersBySeries[c.SeriesId] = bucket = [];
                bucket.Add(await ChapterDto.FromAsync(c, _audio, cancellationToken));
            }

            return new LibraryResponse
            {
                Series = series.Select(s => new SeriesDto
                {
                    Id = s.Id,
                    Title = s.Title,
                    IsActive = s.IsActive,
                    CreatedAt = s.CreatedDate,
                    ChapterCount = chaptersBySeries.TryGetValue(s.Id, out List<ChapterDto>? ch) ? ch.Count : 0,
                    Chapters = chaptersBySeries.GetValueOrDefault(s.Id) ?? []
                }).ToList()
            };
        }
    }
}
