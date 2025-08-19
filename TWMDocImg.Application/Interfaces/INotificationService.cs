using TWMDocImg.Application.DTOs;

namespace TWMDocImg.Application.Interfaces;

public interface INotificationService
{
	Task SendNotificationAsync(NotificationDto notificationDto);
} 