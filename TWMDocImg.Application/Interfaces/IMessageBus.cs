namespace TWMDocImg.Application.Interfaces;

public interface IMessageBus
{
	Task PublishAsync(string topic, string message, CancellationToken cancellationToken = default);
} 