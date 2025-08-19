namespace TWMDocImg.Domain.DomainEvents;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
} 