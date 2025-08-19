using Microsoft.Extensions.DependencyInjection;
using TWMDocImg.Application.Interfaces;
using TWMDocImg.Domain.Services;

namespace TWMDocImg.Infrastructure.Messaging.EventBus;

public static class EventBusExtensions
{
	public static IServiceCollection AddEventBus(this IServiceCollection services)
	{
		services.AddScoped<IMessageBus, KafkaEventBus>();
		services.AddScoped<IDomainEventBus, DomainEventBus>();
		return services;
	}
} 