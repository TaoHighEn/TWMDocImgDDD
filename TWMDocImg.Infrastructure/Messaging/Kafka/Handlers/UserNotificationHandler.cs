using System.Text.Json;
using Microsoft.Extensions.Logging;
using TWMDocImg.Application.DTOs;
using TWMDocImg.Application.Interfaces;
using TWMDocImg.Infrastructure.Messaging.Kafka.Attributes;
using TWMDocImg.Infrastructure.Messaging.Kafka.Interfaces;

namespace TWMDocImg.Infrastructure.Messaging.Kafka.Handlers;

[KafkaTopic("user-notification-topic")]
public class UserNotificationHandler : IMessageHandler
{
	private readonly INotificationService _notificationService;
	private readonly ILogger<UserNotificationHandler> _logger;
	public string TopicName => "user-notification-topic";

	public UserNotificationHandler(INotificationService notificationService, ILogger<UserNotificationHandler> logger)
	{
		_notificationService = notificationService;
		_logger = logger;
	}

	public async Task HandleAsync(string message, CancellationToken cancellationToken = default)
	{
		try
		{
			_logger.LogInformation("開始處理用戶通知訊息");
			var notificationDto = JsonSerializer.Deserialize<NotificationDto>(message);
			if (notificationDto != null)
			{
				await _notificationService.SendNotificationAsync(notificationDto);
				_logger.LogInformation("通知已發送給用戶 {UserId}", notificationDto.UserId);
			}
			else
			{
				_logger.LogWarning("無法反序列化用戶通知訊息");
			}
		}
		catch (JsonException ex)
		{
			_logger.LogError(ex, "反序列化用戶通知訊息時發生錯誤");
			throw;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "處理用戶通知訊息時發生錯誤");
			throw;
		}
	}
} 