using Application.Features.Children.Rules;
using Application.Features.Stories.Dtos;
using Application.Services.CurrentUser;
using Application.Services.Repositories;
using Core.Application.Pipelines.Authorization;
using Core.Application.Responses;
using Domain.Entities.Children;
using Domain.Entities.Stories;
using MediatR;

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
        private readonly IChildRepository _childRepository;
        private readonly IStorySeriesRepository _seriesRepository;
        private readonly IStoryChapterRepository _chapterRepository;
        private readonly ICurrentUser _currentUser;
        private readonly ChildBusinessRules _childBusinessRules;

        public GetSeriesListQueryHandler(
            IChildRepository childRepository,
            IStorySeriesRepository seriesRepository,
            IStoryChapterRepository chapterRepository,
            ICurrentUser currentUser,
            ChildBusinessRules childBusinessRules)
        {
            _childRepository = childRepository;
            _seriesRepository = seriesRepository;
            _chapterRepository = chapterRepository;
            _currentUser = currentUser;
            _childBusinessRules = childBusinessRules;
        }

        public async Task<SeriesListResponse> Handle(GetSeriesListQuery request, CancellationToken cancellationToken)
        {
            long userId = _currentUser.UserIdOrThrow();
            Child? child = await _childRepository.GetByUserIdAsync(userId, cancellationToken);
            await _childBusinessRules.ChildShouldExist(child);

            List<StorySeries> series = await _seriesRepository.GetAllForChildAsync(child!.Id, cancellationToken);
            List<StoryChapter> chapters = await _chapterRepository.GetAllForChildAsync(child.Id, cancellationToken);
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
