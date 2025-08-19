namespace TWMDocImg.Infrastructure.Messaging.Kafka.Interfaces;

public interface IMessageHandlerFactory
{
	IMessageHandler GetHandler(string topicName);
	IEnumerable<string> GetAllTopics();
	bool IsTopicSupported(string topicName);
} 