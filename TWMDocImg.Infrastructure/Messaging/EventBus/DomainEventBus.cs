using System.Text.Json;
using Microsoft.Extensions.Logging;
using TWMDocImg.Application.DTOs;
using TWMDocImg.Application.Interfaces;
using TWMDocImg.Domain.DomainEvents;
using TWMDocImg.Domain.Services;

namespace TWMDocImg.Infrastructure.Messaging.EventBus;

public class DomainEventBus : IDomainEventBus
{
	private readonly IMessageBus _messageBus;
	private readonly ILogger<DomainEventBus> _logger;

	public DomainEventBus(IMessageBus messageBus, ILogger<DomainEventBus> logger)
	{
		_messageBus = messageBus;
		_logger = logger;
	}

	public async Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
	{
		switch (domainEvent)
		{
			case NotificationCreatedEvent notificationEvent:
				await PublishNotificationAsync(notificationEvent, cancellationToken);
				break;
			case OrderProcessedEvent orderProcessedEvent:
				await PublishOrderAsync(orderProcessedEvent, cancellationToken);
				break;
			case DocumentUploadedEvent documentUploadedEvent:
				await PublishDocumentEventAsync(documentUploadedEvent, cancellationToken);
				break;
			default:
				_logger.LogWarning("未支援的領域事件型別: {EventType}", domainEvent.GetType().Name);
				break;
		}
	}

	private async Task PublishNotificationAsync(NotificationCreatedEvent evt, CancellationToken cancellationToken)
	{
		var dto = new NotificationDto
		{
			UserId = evt.UserId,
			Title = evt.Title,
			Message = $"通知: {evt.Title}",
			Type = "Info",
			CreatedAt = evt.OccurredOn
		};
		var payload = JsonSerializer.Serialize(dto);
		await _messageBus.PublishAsync("user-notification-topic", payload, cancellationToken);
	}

	private async Task PublishOrderAsync(OrderProcessedEvent evt, CancellationToken cancellationToken)
	{
		var dto = new OrderProcessDto
		{
			OrderId = evt.OrderId,
			CustomerId = string.Empty,
			TotalAmount = 0,
			Status = evt.Status,
			OrderDate = evt.OccurredOn
		};
		var payload = JsonSerializer.Serialize(dto);
		await _messageBus.PublishAsync("order-processing-topic", payload, cancellationToken);
	}

	private async Task PublishDocumentEventAsync(DocumentUploadedEvent evt, CancellationToken cancellationToken)
	{
		var payload = JsonSerializer.Serialize(new
		{
			Event = "DocumentUploaded",
			FileName = evt.FileName,
			UploadedBy = evt.UploadedBy,
			OccurredOn = evt.OccurredOn
		});
		await _messageBus.PublishAsync("document-events", payload, cancellationToken);
	}
} 