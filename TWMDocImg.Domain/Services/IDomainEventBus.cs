using TWMDocImg.Domain.DomainEvents;

namespace TWMDocImg.Domain.Services;

public interface IDomainEventBus
{
    Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
} 