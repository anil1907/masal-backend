using Application.Features.Children.Commands.Create;
using Application.Features.Children.Commands.Update;
using Application.Features.Children.Queries.GetList;
using Application.Features.Children.Queries.GetMy;
using AutoMapper;
using Domain.Entities.Children;

namespace Application.Features.Children.Profiles;

public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<CreateChildCommand, Child>();
        CreateMap<Child, CreatedChildResponse>();
        CreateMap<Child, UpdatedChildResponse>();
        CreateMap<Child, GetMyChildResponse>();
        CreateMap<Child, ChildListItem>();
    }
}
