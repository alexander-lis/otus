namespace MyApp.Auth.Models;

public class UserDto
{
    public int Id { get; set; }
    public string Login { get; set; } = null!;
    public string Password { get; set; } = null!;
}