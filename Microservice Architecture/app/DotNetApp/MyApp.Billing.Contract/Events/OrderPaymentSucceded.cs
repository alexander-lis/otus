namespace MyApp.Billing.Contract.Events;

public class OrderPaymentSucceded
{
    public int UserId { get; set; }
    public int OrderId { get; set; }
    public int Price { get; set; }
}