using System.Security.Claims;

namespace Core.Security.Extensions;

public static class ClaimExtensions
{
    public static void AddEmail(this ICollection<Claim> claims, string email)
    {
        claims.Add(new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", email));
    }

    public static void AddName(this ICollection<Claim> claims, string name)
    {
        claims.Add(new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", name));
    }

    public static void AddNameIdentifier(this ICollection<Claim> claims, string nameIdentifier)
    {
        claims.Add(
            new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", nameIdentifier));
    }

    public static void AddRoles(this ICollection<Claim> claims, ICollection<string> roles)
    {
        foreach (string role in roles)
            claims.AddRole(role);
    }

    public static void AddRole(this ICollection<Claim> claims, string role)
    {
        claims.Add(new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", role));
    }

    public static void AddUserType(this ICollection<Claim> claims, string userType)
    {
        claims.Add(new Claim("UserType", userType));
    }
}