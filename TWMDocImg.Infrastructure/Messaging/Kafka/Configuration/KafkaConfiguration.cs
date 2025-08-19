namespace TWMDocImg.Infrastructure.Messaging.Kafka.Configuration;

public class KafkaConfiguration
{
	public string BootstrapServers { get; set; } = string.Empty;
	public string GroupId { get; set; } = string.Empty;
	public string TopicName { get; set; } = string.Empty;
	public string AutoOffsetReset { get; set; } = "Earliest";
	public bool EnableAutoCommit { get; set; } = false;
	public int SessionTimeoutMs { get; set; } = 30000;
	public int HeartbeatIntervalMs { get; set; } = 3000;
	public int MaxPollIntervalMs { get; set; } = 300000;
} 