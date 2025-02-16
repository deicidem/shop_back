namespace shop.Models;

public class Order
{
    public int Id { get; set; }
    public List<OrderItem> Items { get; set; } = new();
    public string CustomerEmail { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
}

public class OrderItem
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public enum OrderStatus
{
    Pending = 1,
    Paid = 2,
    Shipped = 3, 
    Completed = 4
}