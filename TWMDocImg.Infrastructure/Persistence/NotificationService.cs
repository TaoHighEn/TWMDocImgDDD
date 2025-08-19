using Microsoft.Extensions.Logging;
using TWMDocImg.Application.DTOs;
using TWMDocImg.Application.Interfaces;

namespace TWMDocImg.Infrastructure.Persistence;

public class NotificationService : INotificationService
{
	private readonly ILogger<NotificationService> _logger;

	public NotificationService(ILogger<NotificationService> logger)
	{
		_logger = logger;
	}

	public Task SendNotificationAsync(NotificationDto notificationDto)
	{
		_logger.LogInformation("[Notification] UserId={UserId}, Title={Title}, Type={Type}", notificationDto.UserId, notificationDto.Title, notificationDto.Type);
		return Task.CompletedTask;
	}
} 