using Microsoft.Extensions.Logging;
using TWMDocImg.Application.DTOs;
using TWMDocImg.Application.Interfaces;

namespace TWMDocImg.Infrastructure.Persistence;

public class OrderProcessingService : IOrderProcessingService
{
	private readonly ILogger<OrderProcessingService> _logger;

	public OrderProcessingService(ILogger<OrderProcessingService> logger)
	{
		_logger = logger;
	}

	public Task ProcessOrderAsync(OrderProcessDto orderDto)
	{
		_logger.LogInformation("[Order] OrderId={OrderId}, CustomerId={CustomerId}, Amount={Amount}, Status={Status}", orderDto.OrderId, orderDto.CustomerId, orderDto.TotalAmount, orderDto.Status);
		return Task.CompletedTask;
	}
} 