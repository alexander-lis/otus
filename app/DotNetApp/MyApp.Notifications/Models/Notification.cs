namespace MyApp.Notifications.Models;

public class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Email { get; set; }
    public string Message { get; set; }
    public DateTime DateTime { get; set; }
}