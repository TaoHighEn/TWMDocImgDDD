namespace TWMDocImg.Domain.DomainEvents;

public class OrderProcessedEvent : IDomainEvent
{
    public string OrderId { get; }
    public string Status { get; }
    public DateTime OccurredOn { get; }

    public OrderProcessedEvent(string orderId, string status)
    {
        OrderId = orderId;
        Status = status;
        OccurredOn = DateTime.UtcNow;
    }
} 