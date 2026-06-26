using Application.Features.Children.Rules;
using Application.Features.Stories.Dtos;
using Application.Persistence;
using Application.Services.CurrentUser;
using Core.Application.Pipelines.Authorization;
using Core.Application.Responses;
using Domain.Entities.Children;
using Domain.Entities.Stories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Stories.Queries.SeriesList;

public class SeriesListResponse : IResponse
{
    public List<SeriesDto> Series { get; set; } = [];
}

/// The child's story series (named arcs), newest first, with chapter counts and which is active.
public class GetSeriesListQuery : IRequest<SeriesListResponse>, ISecuredRequest
{
    public string[] Roles => [OperationClaims.AllowAnonymous];

    public class GetSeriesListQueryHandler : IRequestHandler<GetSeriesListQuery, SeriesListResponse>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUser _currentUser;
        private readonly ChildBusinessRules _childBusinessRules;

        public GetSeriesListQueryHandler(
            IApplicationDbContext db,
            ICurrentUser currentUser,
            ChildBusinessRules childBusinessRules)
        {
            _db = db;
            _currentUser = currentUser;
            _childBusinessRules = childBusinessRules;
        }

        public async Task<SeriesListResponse> Handle(GetSeriesListQuery request, CancellationToken cancellationToken)
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
                .Where(c => c.ChildId == child.Id)
                .OrderByDescending(c => c.Number)
                .ToListAsync(cancellationToken);
            Dictionary<long, int> counts = chapters
                .GroupBy(c => c.SeriesId)
                .ToDictionary(g => g.Key, g => g.Count());

            return new SeriesListResponse
            {
                Series = series.Select(s => new SeriesDto
                {
                    Id = s.Id,
                    Title = s.Title,
                    ChapterCount = counts.GetValueOrDefault(s.Id),
                    IsActive = s.IsActive,
                    CreatedAt = s.CreatedDate
                }).ToList()
            };
        }
    }
}
