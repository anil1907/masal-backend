using Core.Application.Responses;

namespace Application.Features.Children.Queries.GetMy;

public class GetMyChildResponse : IResponse
{
    public long Id { get; set; }
    public string HeroName { get; set; } = default!;
    public List<string> Fears { get; set; } = [];
    public List<string> Interests { get; set; } = [];
    public string? AgeBand { get; set; }
}
