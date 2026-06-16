using Core.Application.Dtos;

namespace Application.Features.Users.Commands.Create;

public class CreatedUserResponse : IDto
{
    public long Id { get; set; }
    
}
