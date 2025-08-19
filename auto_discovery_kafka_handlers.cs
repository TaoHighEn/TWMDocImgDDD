// ==================================================
// 檔案：Interfaces/IMessageHandler.cs
// ==================================================
using System.Threading;
using System.Threading.Tasks;

namespace YourProject.Kafka.Interfaces
{
    /// <summary>
    /// 定義訊息處理器介面
    /// </summary>
    public interface IMessageHandler
    {
        /// <summary>
        /// 處理訊息的方法
        /// </summary>
        /// <param name="message">訊息內容</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns></returns>
        Task HandleAsync(string message, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 對應的Topic名稱
        /// </summary>
        string TopicName { get; }
    }
}

// ==================================================
// 檔案：Interfaces/IMessageHandlerFactory.cs
// ==================================================
using System.Collections.Generic;

namespace YourProject.Kafka.Interfaces
{
    /// <summary>
    /// 訊息處理器工廠介面
    /// </summary>
    public interface IMessageHandlerFactory
    {
        /// <summary>
        /// 根據Topic名稱獲取對應的Handler
        /// </summary>
        /// <param name="topicName">Topic名稱</param>
        /// <returns>訊息處理器</returns>
        IMessageHandler GetHandler(string topicName);
        
        /// <summary>
        /// 獲取所有已註冊的Topic名稱
        /// </summary>
        /// <returns>Topic名稱集合</returns>
        IEnumerable<string> GetAllTopics();
        
        /// <summary>
        /// 檢查是否支援指定的Topic
        /// </summary>
        /// <param name="topicName">Topic名稱</param>
        /// <returns>是否支援</returns>
        bool IsTopicSupported(string topicName);
    }
}

// ==================================================
// 檔案：Attributes/KafkaTopicAttribute.cs
// ==================================================
using System;

namespace YourProject.Kafka.Attributes
{
    /// <summary>
    /// 用於標記Handler對應的Topic的屬性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class KafkaTopicAttribute : Attribute
    {
        public string TopicName { get; }
        
        public KafkaTopicAttribute(string topicName)
        {
            TopicName = topicName ?? throw new ArgumentNullException(nameof(topicName));
        }
    }
}

// ==================================================
// 檔案：DTOs/DocumentUploadDto.cs
// ==================================================
using System;

namespace YourProject.Kafka.DTOs
{
    public class DocumentUploadDto
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public string UploadedBy { get; set; } = string.Empty;
        public DateTime UploadTime { get; set; }
        public byte[]? FileContent { get; set; }
    }
}

// ==================================================
// 檔案：DTOs/NotificationDto.cs
// ==================================================
using System;

namespace YourProject.Kafka.DTOs
{
    public class NotificationDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "Info";
        public DateTime CreatedAt { get; set; }
    }
}

// ==================================================
// 檔案：DTOs/OrderProcessDto.cs
// ==================================================
using System;

namespace YourProject.Kafka.DTOs
{
    public class OrderProcessDto
    {
        public string OrderId { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
    }
}

// ==================================================
// 檔案：Services/IDocumentStorageService.cs
// ==================================================
using System.Threading.Tasks;
using YourProject.Kafka.DTOs;

namespace YourProject.Kafka.Services
{
    public interface IDocumentStorageService
    {
        Task SaveDocumentAsync(DocumentUploadDto documentDto);
    }
}

// ==================================================
// 檔案：Services/INotificationService.cs
// ==================================================
using System.Threading.Tasks;
using YourProject.Kafka.DTOs;

namespace YourProject.Kafka.Services
{
    public interface INotificationService
    {
        Task SendNotificationAsync(NotificationDto notificationDto);
    }
}

// ==================================================
// 檔案：Services/IOrderProcessingService.cs
// ==================================================
using System.Threading.Tasks;
using YourProject.Kafka.DTOs;

namespace YourProject.Kafka.Services
{
    public interface IOrderProcessingService
    {
        Task ProcessOrderAsync(OrderProcessDto orderDto);
    }
}

// ==================================================
// 檔案：Handlers/DocumentUploadHandler.cs
// ==================================================
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using YourProject.Kafka.Attributes;
using YourProject.Kafka.DTOs;
using YourProject.Kafka.Interfaces;
using YourProject.Kafka.Services;

namespace YourProject.Kafka.Handlers
{
    /// <summary>
    /// 文件上傳處理器
    /// </summary>
    [KafkaTopic("twm-doc-img-topic")]
    public class DocumentUploadHandler : IMessageHandler
    {
        private readonly IDocumentStorageService _documentStorageService;
        private readonly ILogger<DocumentUploadHandler> _logger;

        public string TopicName => "twm-doc-img-topic";

        public DocumentUploadHandler(IDocumentStorageService documentStorageService, ILogger<DocumentUploadHandler> logger)
        {
            _documentStorageService = documentStorageService;
            _logger = logger;
        }

        public async Task HandleAsync(string message, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("開始處理文件上傳訊息");
                
                var documentDto = JsonSerializer.Deserialize<DocumentUploadDto>(message);
                if (documentDto != null)
                {
                    await _documentStorageService.SaveDocumentAsync(documentDto);
                    _logger.LogInformation("檔案 {FileName} 已成功處理並儲存", documentDto.FileName);
                }
                else
                {
                    _logger.LogWarning("無法反序列化文件上傳訊息");
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "反序列化文件上傳訊息時發生錯誤");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "處理文件上傳訊息時發生錯誤");
                throw;
            }
        }
    }
}

// ==================================================
// 檔案：Handlers/UserNotificationHandler.cs
// ==================================================
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using YourProject.Kafka.Attributes;
using YourProject.Kafka.DTOs;
using YourProject.Kafka.Interfaces;
using YourProject.Kafka.Services;

namespace YourProject.Kafka.Handlers
{
    /// <summary>
    /// 用戶通知處理器
    /// </summary>
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
}

// ==================================================
// 檔案：Handlers/OrderProcessHandler.cs
// ==================================================
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using YourProject.Kafka.Attributes;
using YourProject.Kafka.DTOs;
using YourProject.Kafka.Interfaces;
using YourProject.Kafka.Services;

namespace YourProject.Kafka.Handlers
{
    /// <summary>
    /// 訂單處理器
    /// </summary>
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
}

// ==================================================
// 檔案：Factories/MessageHandlerFactory.cs
// ==================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using YourProject.Kafka.Attributes;
using YourProject.Kafka.Interfaces;

namespace YourProject.Kafka.Factories
{
    /// <summary>
    /// 自動發現Handler的工廠類別
    /// </summary>
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

        /// <summary>
        /// 自動發現並註冊所有Handler
        /// </summary>
        private void DiscoverAndRegisterHandlers()
        {
            _logger.LogInformation("開始自動發現並註冊Kafka Handler");

            try
            {
                // 獲取所有實作IMessageHandler的類別
                var handlerTypes = Assembly.GetExecutingAssembly()
                    .GetTypes()
                    .Where(t => typeof(IMessageHandler).IsAssignableFrom(t) 
                               && !t.IsInterface 
                               && !t.IsAbstract 
                               && t.IsClass)
                    .ToList();

                _logger.LogInformation("找到 {Count} 個Handler類別", handlerTypes.Count);

                foreach (var handlerType in handlerTypes)
                {
                    try
                    {
                        // 嘗試從屬性獲取Topic名稱
                        var topicAttribute = handlerType.GetCustomAttribute<KafkaTopicAttribute>();
                        string topicName;

                        if (topicAttribute != null)
                        {
                            topicName = topicAttribute.TopicName;
                            _logger.LogDebug("從屬性獲取Topic名稱: {HandlerType} -> {TopicName}", handlerType.Name, topicName);
                        }
                        else
                        {
                            // 如果沒有屬性，嘗試創建實例獲取TopicName
                            var tempHandler = CreateHandlerInstance(handlerType);
                            topicName = tempHandler.TopicName;
                            _logger.LogDebug("從實例獲取Topic名稱: {HandlerType} -> {TopicName}", handlerType.Name, topicName);
                        }

                        if (string.IsNullOrWhiteSpace(topicName))
                        {
                            _logger.LogWarning("Handler {HandlerType} 的Topic名稱為空，跳過註冊", handlerType.Name);
                            continue;
                        }

                        // 檢查是否已經註冊相同Topic
                        if (_handlerTypes.ContainsKey(topicName))
                        {
                            _logger.LogWarning("Topic '{TopicName}' 已經被 {ExistingHandler} 註冊，跳過 {NewHandler}",
                                topicName, _handlerTypes[topicName].Name, handlerType.Name);
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
                
                // 記錄所有已註冊的Topic
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

        /// <summary>
        /// 創建Handler實例（用於獲取Topic名稱）
        /// </summary>
        private IMessageHandler CreateHandlerInstance(Type handlerType)
        {
            try
            {
                // 嘗試從DI容器創建
                return (IMessageHandler)_serviceProvider.GetRequiredService(handlerType);
            }
            catch
            {
                // 如果DI失敗，嘗試直接創建實例（僅用於獲取TopicName）
                var constructors = handlerType.GetConstructors()
                    .OrderBy(c => c.GetParameters().Length)
                    .ToArray();

                foreach (var constructor in constructors)
                {
                    try
                    {
                        var parameters = constructor.GetParameters();
                        var args = new object[parameters.Length];

                        for (int i = 0; i < parameters.Length; i++)
                        {
                            var paramType = parameters[i].ParameterType;
                            try
                            {
                                args[i] = _serviceProvider.GetRequiredService(paramType);
                            }
                            catch
                            {
                                // 使用預設值或null
                                args[i] = paramType.IsValueType ? Activator.CreateInstance(paramType) : null;
                            }
                        }

                        return (IMessageHandler)Activator.CreateInstance(handlerType, args);
                    }
                    catch
                    {
                        continue;
                    }
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
                try
                {
                    var handler = (IMessageHandler)_serviceProvider.GetRequiredService(handlerType);
                    _logger.LogDebug("成功獲取Handler: Topic '{TopicName}' -> {HandlerType}", topicName, handlerType.Name);
                    return handler;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "創建Handler實例時發生錯誤: {HandlerType}", handlerType.Name);
                    throw;
                }
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
}

// ==================================================
// 檔案：Services/KafkaConsumerService.cs
// ==================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YourProject.Kafka.Interfaces;

namespace YourProject.Kafka.Services
{
    /// <summary>
    /// Kafka消費者後台服務
    /// </summary>
    public class KafkaConsumerService : BackgroundService
    {
        private readonly ILogger<KafkaConsumerService> _logger;
        private readonly IConsumer<string, string> _consumer;
        private readonly IServiceProvider _serviceProvider;
        private readonly IMessageHandlerFactory _handlerFactory;
        private readonly List<string> _topicNames;
        private readonly ConsumerConfig _consumerConfig;

        public KafkaConsumerService(
            IConfiguration configuration, 
            ILogger<KafkaConsumerService> logger, 
            IServiceProvider serviceProvider,
            IMessageHandlerFactory handlerFactory)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _handlerFactory = handlerFactory;

            // 讀取Kafka設定
            _consumerConfig = new ConsumerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "172.28.94.105:9092",
                GroupId = configuration["Kafka:GroupId"] ?? "TWMDocImg.ProcessorGroup",
                AutoOffsetReset = Enum.TryParse<AutoOffsetReset>(
                    configuration["Kafka:AutoOffsetReset"], 
                    true, 
                    out var offsetReset) ? offsetReset : AutoOffsetReset.Earliest,
                EnableAutoCommit = bool.TryParse(configuration["Kafka:EnableAutoCommit"], out var autoCommit) && autoCommit,
                // 新增更多設定選項
                SessionTimeoutMs = int.TryParse(configuration["Kafka:SessionTimeoutMs"], out var sessionTimeout) ? sessionTimeout : 30000,
                HeartbeatIntervalMs = int.TryParse(configuration["Kafka:HeartbeatIntervalMs"], out var heartbeat) ? heartbeat : 3000,
                MaxPollIntervalMs = int.TryParse(configuration["Kafka:MaxPollIntervalMs"], out var maxPoll) ? maxPoll : 300000,
            };

            _consumer = new ConsumerBuilder<string, string>(_consumerConfig)
                .SetErrorHandler((_, e) => _logger.LogError("Kafka消費者錯誤: {Reason}", e.Reason))
                .SetStatisticsHandler((_, json) => _logger.LogDebug("Kafka統計資訊: {Statistics}", json))
                .Build();
            
            // 獲取所有需要訂閱的Topic
            _topicNames = _handlerFactory.GetAllTopics().ToList();
            
            if (!_topicNames.Any())
            {
                _logger.LogWarning("沒有找到任何可訂閱的Topic");
            }
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_topicNames.Any())
            {
                _logger.LogError("沒有可訂閱的Topic，Kafka消費者服務無法啟動");
                return Task.CompletedTask;
            }

            _logger.LogInformation("Kafka消費者服務已啟動，訂閱Topics: {Topics}", string.Join(", ", _topicNames));
            _logger.LogInformation("Consumer設定: BootstrapServers={BootstrapServers}, GroupId={GroupId}", 
                _consumerConfig.BootstrapServers, _consumerConfig.GroupId);
            
            Task.Run(async () =>
            {
                try
                {
                    _consumer.Subscribe(_topicNames);
                    _logger.LogInformation("已成功訂閱 {Count} 個Topic", _topicNames.Count);
                    
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        await ProcessMessageAsync(stoppingToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Kafka消費者服務正在停止");
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Kafka消費者服務發生致命錯誤");
                }
                finally
                {
                    try
                    {
                        _consumer.Close();
                        _logger.LogInformation("Kafka消費者已關閉");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "關閉Kafka消費者時發生錯誤");
                    }
                }
            }, stoppingToken);

            return Task.CompletedTask;
        }

        /// <summary>
        /// 處理單個訊息
        /// </summary>
        private async Task ProcessMessageAsync(CancellationToken stoppingToken)
        {
            try
            {
                var consumeResult = _consumer.Consume(stoppingToken);
                
                if (consumeResult?.Message == null)
                {
                    _logger.LogDebug("接收到空訊息，跳過處理");
                    return;
                }

                var topicName = consumeResult.Topic;
                var messageKey = consumeResult.Message.Key;
                var messageValue = consumeResult.Message.Value;
                
                _logger.LogInformation("接收到Kafka訊息: Topic={Topic}, Key={Key}, Partition={Partition}, Offset={Offset}", 
                    topicName, messageKey, consumeResult.Partition.Value, consumeResult.Offset.Value);

                // 檢查是否支援該Topic
                if (!_handlerFactory.IsTopicSupported(topicName))
                {
                    _logger.LogWarning("不支援的Topic: {Topic}，跳過處理", topicName);
                    // 即使不支援也要commit，避免重複消費
                    if (!_consumerConfig.EnableAutoCommit)
                    {
                        _consumer.Commit(consumeResult);
                    }
                    return;
                }

                // 使用新的Service Scope處理訊息
                using var scope = _serviceProvider.CreateScope();
                var scopedHandlerFactory = scope.ServiceProvider.GetRequiredService<IMessageHandlerFactory>();
                var handler = scopedHandlerFactory.GetHandler(topicName);
                
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                
                await handler.HandleAsync(messageValue, stoppingToken);
                
                stopwatch.Stop();
                
                // 手動提交offset
                if (!_consumerConfig.EnableAutoCommit)
                {
                    _consumer.Commit(consumeResult);
                }
                
                _logger.LogInformation("Topic {Topic} 訊息處理完成，耗時: {ElapsedMs}ms", topicName, stopwatch.ElapsedMilliseconds);
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "消費Kafka訊息時發生錯誤: {Error}", ex.Error.Reason);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("處理訊息被取消");
                throw; // 重新拋出以正確處理取消
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "處理Kafka訊息時發生未預期的錯誤");
                
                // 等待一段時間再繼續，避免快速失敗循環
                try
                {
                    await Task.Delay(5000, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // 如果在延遲期間被取消，直接返回
                    throw;
                }
            }
        }

        public override void Dispose()
        {
            try
            {
                _consumer?.Close();
                _consumer?.Dispose();
                _logger.LogInformation("KafkaConsumerService已成功釋放資源");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "釋放KafkaConsumerService資源時發生錯誤");
            }
            finally
            {
                base.Dispose();
            }
        }
    }
}

// ==================================================
// 檔案：Extensions/ServiceCollectionExtensions.cs
// ==================================================
using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using YourProject.Kafka.Factories;
using YourProject.Kafka.Interfaces;
using YourProject.Kafka.Services;

namespace YourProject.Kafka.Extensions
{
    /// <summary>
    /// 服務註冊擴充方法
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 註冊Kafka相關服務和自動發現的Handler
        /// </summary>
        public static IServiceCollection AddKafkaHandlers(this IServiceCollection services)
        {
            // 自動註冊所有Handler
            services.AddKafkaHandlersFromAssembly(Assembly.GetExecutingAssembly());
            
            // 註冊Factory
            services.AddSingleton<IMessageHandlerFactory, MessageHandlerFactory>();
            
            // 註冊Kafka消費者服務
            services.AddHostedService<KafkaConsumerService>();
            
            return services;
        }

        /// <summary>
        /// 從指定Assembly自動註冊所有Handler
        /// </summary>
        public static IServiceCollection AddKafkaHandlersFromAssembly(this IServiceCollection services, Assembly assembly)
        {
            // 找到所有實作IMessageHandler的類別
            var handlerTypes = assembly.GetTypes()
                .Where(t => typeof(IMessageHandler).IsAssignableFrom(t) 
                           && !t.IsInterface 
                           && !t.IsAbstract 
                           && t.IsClass)
                .ToList();

            // 註冊所有Handler為Scoped
            foreach (var handlerType in handlerTypes)
            {
                services.AddScoped(handlerType);
                services.AddScoped(typeof(IMessageHandler), handlerType);
            }

            return services;
        }

        /// <summary>
        /// 註冊業務服務（示例實作）
        /// </summary>
        public static IServiceCollection AddBusinessServices(this IServiceCollection services)
        {
            // 這裡註冊你的業務服務
            services.AddScoped<IDocumentStorageService, DocumentStorageService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IOrderProcessingService, OrderProcessingService>();
            
            return services;
        }
    }
}

// ==================================================
// 檔案：Program.cs (使用範例)
// ==================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YourProject.Kafka.Extensions;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((context, services) =>
{
    // 設定