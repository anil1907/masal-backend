using System.Collections.Immutable;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Core.Security.Encryption;
using Core.Security.Extensions;
using Core.Security.JWT;
using Domain.Entities.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Application.Services.Token;

public class TokenHelper : ITokenHelper
{
    private readonly TokenOptions _tokenOptions;

    public TokenHelper(IOptions<TokenOptions> tokenOptions)
    {
        _tokenOptions = tokenOptions.Value;
    }

    public AccessToken CreateToken(User user, IList<OperationClaim> operationClaims)
    {
        DateTime accessTokenExpiration = DateTime.Now.AddMinutes(_tokenOptions.AccessTokenExpiration);
        SigningCredentials signingCredentials =
            SigningCredentialsHelper.CreateSigningCredentials(
                SecurityKeyHelper.CreateSecurityKey(_tokenOptions.SecurityKey));
        string str = new JwtSecurityTokenHandler().WriteToken(
            CreateJwtSecurityToken(_tokenOptions, user, signingCredentials, operationClaims,
                accessTokenExpiration));
        return new AccessToken()
        {
            Token = str,
            ExpirationDate = accessTokenExpiration
        };
    }

    protected virtual JwtSecurityToken CreateJwtSecurityToken(
        TokenOptions tokenOptions,
        User user,
        SigningCredentials signingCredentials,
        IList<OperationClaim> operationClaims,
        DateTime accessTokenExpiration)
    {
        return new JwtSecurityToken(
            tokenOptions.Issuer,
            tokenOptions.Audience,
            SetClaims(user, operationClaims),
            DateTime.Now,
            accessTokenExpiration,
            signingCredentials);
    }

    protected virtual IEnumerable<Claim> SetClaims(
        User user,
        IList<OperationClaim> operationClaims)
    {
        List<Claim> claimList = [];
        claimList.AddNameIdentifier(user.Id.ToString());
        claimList.AddName(user.Username);
        claimList.AddEmail(user.Email);
        claimList.AddRoles(operationClaims.Select(c => c.Name).ToArray());
        return claimList.ToImmutableList();
    }
}
