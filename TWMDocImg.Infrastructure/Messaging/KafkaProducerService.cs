using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TWMDocImg.Application.DTOs;
using TWMDocImg.Application.Interfaces;

namespace TWMDocImg.Infrastructure.Messaging;

public class KafkaProducerService : IFileQueueService
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducerService> _logger;
    private readonly string _topicName;

    public KafkaProducerService(IConfiguration configuration, ILogger<KafkaProducerService> logger)
    {
        _logger = logger;
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "172.28.94.105:9092",
            // 在此處可加入更多安全性或效能設定
        };
        _producer = new ProducerBuilder<string, string>(producerConfig).Build();
        _topicName = configuration["Kafka:TopicName"] ?? "twm-doc-img-topic";
    }

    public async Task QueueFileForProcessingAsync(DocumentUploadDto document)
    {
        try
        {
            var message = JsonSerializer.Serialize(document);
            var deliveryResult = await _producer.ProduceAsync(_topicName, new Message<string, string> { Key = document.FileId.ToString(), Value = message });
            _logger.LogInformation("訊息已傳送到 Kafka Topic: {Topic}, Partition: {Partition}, Offset: {Offset}",
                deliveryResult.Topic, deliveryResult.Partition, deliveryResult.Offset);
        }
        catch (ProduceException<string, string> e)
        {
            _logger.LogError("傳送 Kafka 訊息失敗: {Reason}", e.Error.Reason);
            throw;
        }
    }
}