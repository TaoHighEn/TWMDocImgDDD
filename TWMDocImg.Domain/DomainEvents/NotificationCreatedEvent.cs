namespace TWMDocImg.Domain.DomainEvents;

public class NotificationCreatedEvent : IDomainEvent
{
    public string UserId { get; }
    public string Title { get; }
    public DateTime OccurredOn { get; }

    public NotificationCreatedEvent(string userId, string title)
    {
        UserId = userId;
        Title = title;
        OccurredOn = DateTime.UtcNow;
    }
} 