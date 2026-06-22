using Core.Application.Responses;

namespace Application.Features.Children.Queries.GetList;

public class GetChildrenResponse : IResponse
{
    public List<ChildListItem> Children { get; set; } = [];
}

public class ChildListItem
{
    public long Id { get; set; }
    public string HeroName { get; set; } = default!;
    public List<string> Fears { get; set; } = [];
    public List<string> Interests { get; set; } = [];
    public string? AgeBand { get; set; }
    public string? Gender { get; set; }
    public bool IsActive { get; set; }
}
