using System;

namespace TWMDocImg.Infrastructure.Messaging.Kafka.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class KafkaTopicAttribute : Attribute
{
	public string TopicName { get; }

	public KafkaTopicAttribute(string topicName)
	{
		TopicName = topicName ?? throw new ArgumentNullException(nameof(topicName));
	}
} 