using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TWMDocImg.Application.Interfaces;

namespace TWMDocImg.Infrastructure.Messaging.EventBus;

public class KafkaEventBus : IMessageBus
{
	private readonly IProducer<string, string> _producer;
	private readonly ILogger<KafkaEventBus> _logger;

	public KafkaEventBus(IConfiguration configuration, ILogger<KafkaEventBus> logger)
	{
		_logger = logger;
		var producerConfig = new ProducerConfig
		{
			BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "172.28.94.105:9092",
		};
		_producer = new ProducerBuilder<string, string>(producerConfig).Build();
	}

	public async Task PublishAsync(string topic, string message, CancellationToken cancellationToken = default)
	{
		try
		{
			var deliveryResult = await _producer.ProduceAsync(topic, new Message<string, string> { Key = Guid.NewGuid().ToString(), Value = message }, cancellationToken);
			_logger.LogInformation("EventBus 發佈成功: Topic={Topic}, Partition={Partition}, Offset={Offset}", deliveryResult.Topic, deliveryResult.Partition, deliveryResult.Offset);
		}
		catch (ProduceException<string, string> e)
		{
			_logger.LogError("EventBus 發佈失敗: {Reason}", e.Error.Reason);
			throw;
		}
	}
} 