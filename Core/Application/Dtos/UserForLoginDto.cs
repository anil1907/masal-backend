namespace Core.Application.Dtos;

public class UserForLoginDto: IDto
{
    public string Username { get; set; }

    public string Password { get; set; }


    public UserForLoginDto()
    {
        Username = string.Empty;
        Password = string.Empty;
    }

    public UserForLoginDto(string username, string password)
    {
        Username = username;
        Password = password;
    }
}
