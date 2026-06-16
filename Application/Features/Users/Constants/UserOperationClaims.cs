namespace Application.Features.Users.Constants;

public static class UserOperationClaims
{
    private const string _section = "User";

    public const string Admin = $"{_section}.Admin";
    public const string Create = $"{_section}.Create";
    public const string Update = $"{_section}.Update";
    public const string Delete = $"{_section}.Delete";
    public const string View = $"{_section}.View";
    public const string List = $"{_section}.List";
}
