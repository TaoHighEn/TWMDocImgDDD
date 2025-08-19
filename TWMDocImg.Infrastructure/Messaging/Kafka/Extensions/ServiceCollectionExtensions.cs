using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using TWMDocImg.Application.Interfaces;
using TWMDocImg.Infrastructure.Messaging.Kafka.Factories;
using TWMDocImg.Infrastructure.Messaging.Kafka.Interfaces;
using TWMDocImg.Infrastructure.Messaging.Kafka.Services;
using TWMDocImg.Infrastructure.Persistence;

namespace TWMDocImg.Infrastructure.Messaging.Kafka.Extensions;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddKafkaHandlers(this IServiceCollection services)
	{
		services.AddKafkaHandlersFromAssembly(Assembly.GetExecutingAssembly());
		services.AddScoped<IMessageHandlerFactory, MessageHandlerFactory>();
		services.AddHostedService<KafkaConsumerService>();
		return services;
	}

	public static IServiceCollection AddKafkaHandlersFromAssembly(this IServiceCollection services, Assembly assembly)
	{
		var handlerTypes = assembly.GetTypes()
			.Where(t => typeof(IMessageHandler).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract && t.IsClass)
			.ToList();
		foreach (var handlerType in handlerTypes)
		{
			services.AddScoped(handlerType);
			services.AddScoped(typeof(IMessageHandler), handlerType);
		}
		return services;
	}

	public static IServiceCollection AddBusinessServices(this IServiceCollection services)
	{
		services.AddScoped<IDocumentStorageService, DocumentStorageService>();
		services.AddScoped<INotificationService, NotificationService>();
		services.AddScoped<IOrderProcessingService, OrderProcessingService>();
		return services;
	}
} 