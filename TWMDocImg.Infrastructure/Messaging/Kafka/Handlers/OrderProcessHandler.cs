using System.Text.Json;
using Microsoft.Extensions.Logging;
using TWMDocImg.Application.DTOs;
using TWMDocImg.Application.Interfaces;
using TWMDocImg.Infrastructure.Messaging.Kafka.Attributes;
using TWMDocImg.Infrastructure.Messaging.Kafka.Interfaces;

namespace TWMDocImg.Infrastructure.Messaging.Kafka.Handlers;

[KafkaTopic("order-processing-topic")]
public class OrderProcessHandler : IMessageHandler
{
	private readonly IOrderProcessingService _orderProcessingService;
	private readonly ILogger<OrderProcessHandler> _logger;
	public string TopicName => "order-processing-topic";

	public OrderProcessHandler(IOrderProcessingService orderProcessingService, ILogger<OrderProcessHandler> logger)
	{
		_orderProcessingService = orderProcessingService;
		_logger = logger;
	}

	public async Task HandleAsync(string message, CancellationToken cancellationToken = default)
	{
		try
		{
			_logger.LogInformation("開始處理訂單訊息");
			var orderDto = JsonSerializer.Deserialize<OrderProcessDto>(message);
			if (orderDto != null)
			{
				await _orderProcessingService.ProcessOrderAsync(orderDto);
				_logger.LogInformation("訂單 {OrderId} 已成功處理", orderDto.OrderId);
			}
			else
			{
				_logger.LogWarning("無法反序列化訂單訊息");
			}
		}
		catch (JsonException ex)
		{
			_logger.LogError(ex, "反序列化訂單訊息時發生錯誤");
			throw;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "處理訂單訊息時發生錯誤");
			throw;
		}
	}
} 