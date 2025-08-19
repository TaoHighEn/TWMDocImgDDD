namespace TWMDocImg.Infrastructure.Messaging.Kafka.Interfaces;

public interface IMessageHandler
{
	Task HandleAsync(string message, CancellationToken cancellationToken = default);
	string TopicName { get; }
} 