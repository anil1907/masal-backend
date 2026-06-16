using Core.Application.Responses;

namespace Application.Features.Children.Commands.Update;

public class UpdatedChildResponse : IResponse
{
    public long Id { get; set; }
    public string HeroName { get; set; } = default!;
    public List<string> Fears { get; set; } = [];
    public List<string> Interests { get; set; } = [];
    public string? AgeBand { get; set; }
}
