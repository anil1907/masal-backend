using Core.Application.Dtos;

namespace Application.Features.Users.Queries.GetList;

public class GetListUserListItemDto : IDto
{
    public long Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
}
