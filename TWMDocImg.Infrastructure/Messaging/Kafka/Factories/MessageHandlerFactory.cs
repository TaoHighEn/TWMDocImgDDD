using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TWMDocImg.Infrastructure.Messaging.Kafka.Attributes;
using TWMDocImg.Infrastructure.Messaging.Kafka.Interfaces;

namespace TWMDocImg.Infrastructure.Messaging.Kafka.Factories;

public class MessageHandlerFactory : IMessageHandlerFactory
{
	private readonly IServiceProvider _serviceProvider;
	private readonly ILogger<MessageHandlerFactory> _logger;
	private readonly Dictionary<string, Type> _handlerTypes;

	public MessageHandlerFactory(IServiceProvider serviceProvider, ILogger<MessageHandlerFactory> logger)
	{
		_serviceProvider = serviceProvider;
		_logger = logger;
		_handlerTypes = new Dictionary<string, Type>();
		DiscoverAndRegisterHandlers();
	}

	private void DiscoverAndRegisterHandlers()
	{
		_logger.LogInformation("開始自動發現並註冊Kafka Handler");
		try
		{
			var handlerTypes = Assembly.GetExecutingAssembly()
				.GetTypes()
				.Where(t => typeof(IMessageHandler).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract && t.IsClass)
				.ToList();

			_logger.LogInformation("找到 {Count} 個Handler類別", handlerTypes.Count);

			foreach (var handlerType in handlerTypes)
			{
				try
				{
					var topicAttribute = handlerType.GetCustomAttribute<KafkaTopicAttribute>();
					string topicName;

					if (topicAttribute != null)
					{
						topicName = topicAttribute.TopicName;
						_logger.LogDebug("從屬性獲取Topic名稱: {HandlerType} -> {TopicName}", handlerType.Name, topicName);
					}
					else
					{
						var tempHandler = CreateHandlerInstance(handlerType);
						topicName = tempHandler.TopicName;
						_logger.LogDebug("從實例獲取Topic名稱: {HandlerType} -> {TopicName}", handlerType.Name, topicName);
					}

					if (string.IsNullOrWhiteSpace(topicName))
					{
						_logger.LogWarning("Handler {HandlerType} 的Topic名稱為空，跳過註冊", handlerType.Name);
						continue;
					}

					if (_handlerTypes.ContainsKey(topicName))
					{
						_logger.LogWarning("Topic '{TopicName}' 已經被 {ExistingHandler} 註冊，跳過 {NewHandler}", topicName, _handlerTypes[topicName].Name, handlerType.Name);
						continue;
					}

					_handlerTypes[topicName] = handlerType;
					_logger.LogInformation("成功註冊Handler: {HandlerType} -> Topic: {TopicName}", handlerType.Name, topicName);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "註冊Handler {HandlerType} 時發生錯誤", handlerType.Name);
				}
			}

			_logger.LogInformation("Handler註冊完成，總計註冊 {Count} 個Handler", _handlerTypes.Count);
			foreach (var kvp in _handlerTypes)
			{
				_logger.LogDebug("已註冊: Topic '{TopicName}' -> Handler '{HandlerName}'", kvp.Key, kvp.Value.Name);
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "自動發現Handler時發生錯誤");
			throw;
		}
	}

	private IMessageHandler CreateHandlerInstance(Type handlerType)
	{
		try
		{
			return (IMessageHandler)_serviceProvider.GetRequiredService(handlerType);
		}
		catch
		{
			var constructors = handlerType.GetConstructors().OrderBy(c => c.GetParameters().Length).ToArray();
			foreach (var constructor in constructors)
			{
				try
				{
					var parameters = constructor.GetParameters();
					var args = new object[parameters.Length];
					for (int i = 0; i < parameters.Length; i++)
					{
						var paramType = parameters[i].ParameterType;
						try { args[i] = _serviceProvider.GetRequiredService(paramType); }
						catch { args[i] = paramType.IsValueType ? Activator.CreateInstance(paramType) : null; }
					}
					return (IMessageHandler)Activator.CreateInstance(handlerType, args)!;
				}
				catch { continue; }
			}
			throw new InvalidOperationException($"無法創建Handler實例: {handlerType.Name}");
		}
	}

	public IMessageHandler GetHandler(string topicName)
	{
		if (string.IsNullOrWhiteSpace(topicName))
		{
			throw new ArgumentException("Topic名稱不能為空", nameof(topicName));
		}
		if (_handlerTypes.TryGetValue(topicName, out var handlerType))
		{
			var handler = (IMessageHandler)_serviceProvider.GetRequiredService(handlerType);
			_logger.LogDebug("成功獲取Handler: Topic '{TopicName}' -> {HandlerType}", topicName, handlerType.Name);
			return handler;
		}
		var availableTopics = string.Join(", ", _handlerTypes.Keys);
		var errorMessage = $"找不到處理Topic '{topicName}' 的Handler。可用的Topic: [{availableTopics}]";
		_logger.LogError(errorMessage);
		throw new InvalidOperationException(errorMessage);
	}

	public IEnumerable<string> GetAllTopics()
	{
		return _handlerTypes.Keys.ToList();
	}

	public bool IsTopicSupported(string topicName)
	{
		return !string.IsNullOrWhiteSpace(topicName) && _handlerTypes.ContainsKey(topicName);
	}
} 