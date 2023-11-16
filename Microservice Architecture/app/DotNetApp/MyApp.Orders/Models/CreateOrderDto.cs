namespace MyApp.Orders.Models;

public class CreateOrderDto
{
    public int UserId { get; set; }
    public int ScooterStatusId { get; set; }
    public string Title { get; set; }
    public int Price { get; set; }
}