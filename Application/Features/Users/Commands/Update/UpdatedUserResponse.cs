using Core.Application.Dtos;

namespace Application.Features.Users.Commands.Update;

public class UpdatedUserResponse : IDto
{
    public long Id { get; set; }
   
}
