using System.Collections.Immutable;
using System.Security.Claims;

namespace Core.Security.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static ICollection<string>? GetClaims(
        this ClaimsPrincipal claimsPrincipal,
        string claimType)
    {
        ImmutableArray<string>? claims;
        if (claimsPrincipal == null)
        {
            claims = new ImmutableArray<string>?();
        }
        else
        {
            IEnumerable<Claim> all = claimsPrincipal.FindAll(claimType);
            claims = all != null ? new ImmutableArray<string>?(all.Select<Claim, string>((Func<Claim, string>) (x => x.Value)).ToImmutableArray<string>()) : new ImmutableArray<string>?();
        }
        return claims;
    }
        
    public static List<string>? ClaimRoles(this ClaimsPrincipal claimsPrincipal)
    {
        return claimsPrincipal?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
    }

    public static ICollection<string>? GetRoleClaims(this ClaimsPrincipal claimsPrincipal)
    {
        return claimsPrincipal == null ? null : claimsPrincipal.GetClaims("http://schemas.microsoft.com/ws/2008/06/identity/claims/role");
    }

    public static string? GetIdClaim(this ClaimsPrincipal claimsPrincipal)
    {
        return claimsPrincipal?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
    }
}