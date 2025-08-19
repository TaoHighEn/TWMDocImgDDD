# DDD專案中Kafka檔案的重新分配建議

## 1. Domain Layer (TWMDocImg.Domain)
```
TWMDocImg.Domain/
├── Entities/
│   └── (現有實體保持不變)
├── ValueObjects/
│   ├── DocumentUpload.cs      # 從 DTOs/DocumentUploadDto.cs 轉換
│   ├── Notification.cs        # 從 DTOs/NotificationDto.cs 轉換
│   └── OrderProcess.cs        # 從 DTOs/OrderProcessDto.cs 轉換
├── DomainEvents/              # 新增領域事件
│   ├── DocumentUploadedEvent.cs
│   ├── NotificationCreatedEvent.cs
│   └── OrderProcessedEvent.cs
└── Services/
    └── IDomainEventBus.cs     # 領域事件匯流排介面
```

**說明：** DTOs 應該轉換為 Value Objects，因為它們代表領域概念。領域事件可以替代或補充 Kafka 訊息。

## 2. Application Layer (TWMDocImg.Application)
```
TWMDocImg.Application/
├── Commands/                  # 新增命令
│   ├── ProcessDocumentUploadCommand.cs
│   ├── SendNotificationCommand.cs
│   └── ProcessOrderCommand.cs
├── CommandHandlers/           # 新增命令處理器
│   ├── ProcessDocumentUploadCommandHandler.cs
│   ├── SendNotificationCommandHandler.cs
│   └── ProcessOrderCommandHandler.cs
├── DTOs/                      # 應用層 DTOs
│   ├── DocumentUploadDto.cs   # 保留用於 API 傳輸
│   ├── NotificationDto.cs     # 保留用於 API 傳輸
│   └── OrderProcessDto.cs     # 保留用於 API 傳輸
├── Interfaces/
│   ├── IDocumentStorageService.cs     # 從 Services/ 移動
│   ├── INotificationService.cs        # 從 Services/ 移動
│   ├── IOrderProcessingService.cs     # 從 Services/ 移動
│   └── IMessageBus.cs                 # 新增訊息匯流排抽象
└── Services/
    ├── DocumentApplicationService.cs   # 應用服務
    ├── NotificationApplicationService.cs
    └── OrderApplicationService.cs
```

**說明：** 應用層負責協調領域物件和基礎設施，包含命令處理和應用服務。

## 3. Infrastructure Layer (TWMDocImg.Infrastructure)
```
TWMDocImg.Infrastructure/
├── Messaging/
│   ├── Kafka/
│   │   ├── Attributes/
│   │   │   └── KafkaTopicAttribute.cs
│   │   ├── Configuration/
│   │   │   └── KafkaConfiguration.cs      # 新增配置類
│   │   ├── Handlers/
│   │   │   ├── DocumentUploadHandler.cs
│   │   │   ├── UserNotificationHandler.cs
│   │   │   └── OrderProcessHandler.cs
│   │   ├── Interfaces/
│   │   │   ├── IMessageHandler.cs
│   │   │   └── IMessageHandlerFactory.cs
│   │   ├── Factories/
│   │   │   └── MessageHandlerFactory.cs
│   │   ├── Services/
│   │   │   └── KafkaConsumerService.cs
│   │   └── Extensions/
│   │       └── ServiceCollectionExtensions.cs
│   └── EventBus/
│       ├── KafkaEventBus.cs              # 實作 IMessageBus
│       └── EventBusExtensions.cs
├── Persistence/
│   └── Services/
│       ├── DocumentStorageService.cs     # 具體實作
│       ├── NotificationService.cs        # 具體實作
│       └── OrderProcessingService.cs     # 具體實作
└── DependencyInjection/
    └── InfrastructureServiceExtensions.cs
```

**說明：** 基礎設施層包含所有技術實作細節，Kafka 相關程式碼都放在這裡。

## 4. Presentation Layer (TWMDocImg.API)
```
TWMDocImg.API/
├── Controllers/
│   ├── DocumentController.cs
│   ├── NotificationController.cs
│   └── OrderController.cs
├── Configuration/
│   └── KafkaConfiguration.cs             # API 層的 Kafka 配置
└── Program.cs                            # 修改現有的 Program.cs
```

## 5. 重構建議

### A. Handler 重構
將現有的 Kafka Handler 重構為中介者模式：

```csharp
// Infrastructure/Messaging/Kafka/Handlers/DocumentUploadHandler.cs
[KafkaTopic("twm-doc-img-topic")]
public class DocumentUploadHandler : IMessageHandler
{
    private readonly IMediator _mediator;
    private readonly ILogger<DocumentUploadHandler> _logger;

    public async Task HandleAsync(string message, CancellationToken cancellationToken = default)
    {
        var documentDto = JsonSerializer.Deserialize<DocumentUploadDto>(message);
        var command = new ProcessDocumentUploadCommand(documentDto);
        await _mediator.Send(command, cancellationToken);
    }
}
```

### B. 領域事件整合
```csharp
// Domain/DomainEvents/DocumentUploadedEvent.cs
public class DocumentUploadedEvent : IDomainEvent
{
    public string FileName { get; }
    public string UploadedBy { get; }
    public DateTime OccurredOn { get; }
    
    public DocumentUploadedEvent(string fileName, string uploadedBy)
    {
        FileName = fileName;
        UploadedBy = uploadedBy;
        OccurredOn = DateTime.UtcNow;
    }
}
```

### C. 依賴注入重組
```csharp
// Infrastructure/DependencyInjection/InfrastructureServiceExtensions.cs
public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Kafka 服務
        services.AddKafkaHandlers();
        
        // 業務服務實作
        services.AddBusinessServices();
        
        // 事件匯流排
        services.AddScoped<IMessageBus, KafkaEventBus>();
        
        return services;
    }
}
```

## 6. 檔案移動清單

| 原位置 | 新位置 | 說明 |
|--------|--------|------|
| `DTOs/DocumentUploadDto.cs` | `Domain/ValueObjects/DocumentUpload.cs` + `Application/DTOs/DocumentUploadDto.cs` | 拆分為領域值物件和應用層 DTO |
| `DTOs/NotificationDto.cs` | `Domain/ValueObjects/Notification.cs` + `Application/DTOs/NotificationDto.cs` | 同上 |
| `DTOs/OrderProcessDto.cs` | `Domain/ValueObjects/OrderProcess.cs` + `Application/DTOs/OrderProcessDto.cs` | 同上 |
| `Services/I*Service.cs` | `Application/Interfaces/I*Service.cs` | 移動到應用層 |
| `Services/*Service.cs` | `Infrastructure/Persistence/Services/*Service.cs` | 具體實作移到基礎設施層 |
| `Handlers/*` | `Infrastructure/Messaging/Kafka/Handlers/*` | Kafka 處理器移到基礎設施層 |
| `Interfaces/*` | `Infrastructure/Messaging/Kafka/Interfaces/*` | Kafka 介面移到基礎設施層 |
| `Factories/*` | `Infrastructure/Messaging/Kafka/Factories/*` | 工廠類別移到基礎設施層 |
| `Attributes/*` | `Infrastructure/Messaging/Kafka/Attributes/*` | 屬性移到基礎設施層 |
| `Extensions/*` | `Infrastructure/Messaging/Kafka/Extensions/*` | 擴展方法移到基礎設施層 |

## 7. 額外建議

1. **引入 MediatR**：用於 CQRS 和中介者模式
2. **領域事件**：使用領域事件來觸發 Kafka 訊息發送
3. **配置分離**：將 Kafka 配置從程式碼中分離
4. **測試友好**：基礎設施抽象使單元測試更容易
5. **關注點分離**：每層只關注自己的職責

這樣的重組符合 DDD 的分層架構原則，並提高了程式碼的可維護性和測試性。