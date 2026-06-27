using Core.Application.Responses;

namespace Application.Features.Account.Commands.DeleteAccount;

public class DeletedAccountResponse : IResponse
{
    public bool Deleted { get; set; }
}
