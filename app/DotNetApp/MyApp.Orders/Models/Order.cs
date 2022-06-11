namespace MyApp.Orders.Models;

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ScooterStatusId { get; set; }
    public string Title { get; set; }
}