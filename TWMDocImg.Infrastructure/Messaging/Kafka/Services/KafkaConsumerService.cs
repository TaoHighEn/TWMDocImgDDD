using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TWMDocImg.Infrastructure.Messaging.Kafka.Interfaces;

namespace TWMDocImg.Infrastructure.Messaging.Kafka.Services;

public class KafkaConsumerService : BackgroundService
{
	private readonly ILogger<KafkaConsumerService> _logger;
	private readonly IConsumer<string, string> _consumer;
	private readonly IServiceProvider _serviceProvider;
	private readonly List<string> _topicNames;
	private readonly ConsumerConfig _consumerConfig;

	public KafkaConsumerService(
		IConfiguration configuration,
		ILogger<KafkaConsumerService> logger,
		IServiceProvider serviceProvider)
	{
		_logger = logger;
		_serviceProvider = serviceProvider;

		_consumerConfig = new ConsumerConfig
		{
			BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "172.28.94.105:9092",
			GroupId = configuration["Kafka:GroupId"] ?? "TWMDocImg.ProcessorGroup",
			AutoOffsetReset = Enum.TryParse<AutoOffsetReset>(configuration["Kafka:AutoOffsetReset"], true, out var offsetReset) ? offsetReset : AutoOffsetReset.Earliest,
			EnableAutoCommit = bool.TryParse(configuration["Kafka:EnableAutoCommit"], out var autoCommit) && autoCommit,
			SessionTimeoutMs = int.TryParse(configuration["Kafka:SessionTimeoutMs"], out var sessionTimeout) ? sessionTimeout : 30000,
			HeartbeatIntervalMs = int.TryParse(configuration["Kafka:HeartbeatIntervalMs"], out var heartbeat) ? heartbeat : 3000,
			MaxPollIntervalMs = int.TryParse(configuration["Kafka:MaxPollIntervalMs"], out var maxPoll) ? maxPoll : 300000,
		};

		_consumer = new ConsumerBuilder<string, string>(_consumerConfig)
			.SetErrorHandler((_, e) => _logger.LogError("Kafka消費者錯誤: {Reason}", e.Reason))
			.SetStatisticsHandler((_, json) => _logger.LogDebug("Kafka統計資訊: {Statistics}", json))
			.Build();

		using (var scope = _serviceProvider.CreateScope())
		{
			var handlerFactory = scope.ServiceProvider.GetRequiredService<IMessageHandlerFactory>();
			_topicNames = handlerFactory.GetAllTopics().ToList();
		}
		if (!_topicNames.Any())
		{
			_logger.LogWarning("沒有找到任何可訂閱的Topic");
		}
	}

	protected override Task ExecuteAsync(CancellationToken stoppingToken)
	{
		if (!_topicNames.Any())
		{
			_logger.LogError("沒有可訂閱的Topic，Kafka消費者服務無法啟動");
			return Task.CompletedTask;
		}

		_logger.LogInformation("Kafka消費者服務已啟動，訂閱Topics: {Topics}", string.Join(", ", _topicNames));
		_logger.LogInformation("Consumer設定: BootstrapServers={BootstrapServers}, GroupId={GroupId}", _consumerConfig.BootstrapServers, _consumerConfig.GroupId);

		Task.Run(async () =>
		{
			try
			{
				await EnsureTopicsExistAsync(stoppingToken);
				_consumer.Subscribe(_topicNames);
				_logger.LogInformation("已成功訂閱 {Count} 個Topic", _topicNames.Count);
				while (!stoppingToken.IsCancellationRequested)
				{
					await ProcessMessageAsync(stoppingToken);
				}
			}
			catch (OperationCanceledException)
			{
				_logger.LogInformation("Kafka消費者服務正在停止");
			}
			catch (Exception ex)
			{
				_logger.LogCritical(ex, "Kafka消費者服務發生致命錯誤");
			}
			finally
			{
				try
				{
					_consumer.Close();
					_logger.LogInformation("Kafka消費者已關閉");
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "關閉Kafka消費者時發生錯誤");
				}
			}
		}, stoppingToken);

		return Task.CompletedTask;
	}

	private async Task EnsureTopicsExistAsync(CancellationToken cancellationToken)
	{
		try
		{
			var adminConfig = new AdminClientConfig { BootstrapServers = _consumerConfig.BootstrapServers };
			using var admin = new AdminClientBuilder(adminConfig).Build();
			var metadata = admin.GetMetadata(TimeSpan.FromSeconds(5));
			var existing = new HashSet<string>(metadata.Topics.Select(t => t.Topic));
			var missing = _topicNames.Where(t => !existing.Contains(t)).Distinct().ToList();
			if (!missing.Any()) return;

			_logger.LogWarning("偵測到缺少的 Topics: {Topics}，嘗試自動建立", string.Join(", ", missing));
			var specs = missing.Select(t => new TopicSpecification
			{
				Name = t,
				NumPartitions = 1,
				ReplicationFactor = 1
			}).ToList();
			await admin.CreateTopicsAsync(specs);
			_logger.LogInformation("Topics 建立完成: {Topics}", string.Join(", ", missing));
		}
		catch (CreateTopicsException ex)
		{
			// 若 Topic 已被其他節點同時建立，忽略已存在錯誤
			if (ex.Results.All(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
			{
				_logger.LogInformation("Topics 已存在，略過建立");
				return;
			}
			_logger.LogError(ex, "建立 Topics 時發生錯誤");
			throw;
		}
	}

	private async Task ProcessMessageAsync(CancellationToken stoppingToken)
	{
		try
		{
			var consumeResult = _consumer.Consume(stoppingToken);
			if (consumeResult?.Message == null)
			{
				_logger.LogDebug("接收到空訊息，跳過處理");
				return;
			}

			var topicName = consumeResult.Topic;
			var messageKey = consumeResult.Message.Key;
			var messageValue = consumeResult.Message.Value;

			_logger.LogInformation("接收到Kafka訊息: Topic={Topic}, Key={Key}, Partition={Partition}, Offset={Offset}", topicName, messageKey, consumeResult.Partition.Value, consumeResult.Offset.Value);

			using var scope = _serviceProvider.CreateScope();
			var scopedFactory = scope.ServiceProvider.GetRequiredService<IMessageHandlerFactory>();
			if (!scopedFactory.IsTopicSupported(topicName))
			{
				_logger.LogWarning("不支援的Topic: {Topic}，跳過處理", topicName);
				if (_consumerConfig.EnableAutoCommit == false)
				{
					_consumer.Commit(consumeResult);
				}
				return;
			}
			var handler = scopedFactory.GetHandler(topicName);

			var stopwatch = System.Diagnostics.Stopwatch.StartNew();
			await handler.HandleAsync(messageValue, stoppingToken);
			stopwatch.Stop();

			if (_consumerConfig.EnableAutoCommit == false)
			{
				_consumer.Commit(consumeResult);
			}
			_logger.LogInformation("Topic {Topic} 訊息處理完成，耗時: {ElapsedMs}ms", topicName, stopwatch.ElapsedMilliseconds);
		}
		catch (ConsumeException ex)
		{
			_logger.LogError(ex, "消費Kafka訊息時發生錯誤: {Error}", ex.Error.Reason);
		}
		catch (OperationCanceledException)
		{
			_logger.LogInformation("處理訊息被取消");
			throw;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "處理Kafka訊息時發生未預期的錯誤");
			try { await Task.Delay(5000, stoppingToken); } catch (OperationCanceledException) { throw; }
		}
	}

	public override void Dispose()
	{
		try
		{
			_consumer?.Close();
			_consumer?.Dispose();
			_logger.LogInformation("KafkaConsumerService已成功釋放資源");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "釋放KafkaConsumerService資源時發生錯誤");
		}
		finally
		{
			base.Dispose();
		}
	}
} 