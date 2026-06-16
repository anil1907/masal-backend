using Core.Security.JWT;
using Domain.Entities.Users;

namespace Application.Services.Token;

public interface ITokenHelper
{
    AccessToken CreateToken(User user, IList<OperationClaim> operationClaims);
}
