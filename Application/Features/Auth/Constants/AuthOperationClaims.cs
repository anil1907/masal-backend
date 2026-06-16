namespace Application.Features.Auth.Constants;

public static class AuthOperationClaims
{
    private const string _section = "Auth";

    public const string Admin = $"{_section}.Admin";
    public const string Login = $"{_section}.Login";
    public const string Logout = $"{_section}.Logout";
    public const string RefreshToken = $"{_section}.RefreshToken";
}
