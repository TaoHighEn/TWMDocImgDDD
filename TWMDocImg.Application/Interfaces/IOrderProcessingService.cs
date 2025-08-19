using TWMDocImg.Application.DTOs;

namespace TWMDocImg.Application.Interfaces;

public interface IOrderProcessingService
{
	Task ProcessOrderAsync(OrderProcessDto orderDto);
} 