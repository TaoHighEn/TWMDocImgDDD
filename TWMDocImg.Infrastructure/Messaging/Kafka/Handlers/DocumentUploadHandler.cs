using System.Text.Json;
using Microsoft.Extensions.Logging;
using TWMDocImg.Application.DTOs;
using TWMDocImg.Application.Interfaces;
using TWMDocImg.Infrastructure.Messaging.Kafka.Attributes;
using TWMDocImg.Infrastructure.Messaging.Kafka.Interfaces;

namespace TWMDocImg.Infrastructure.Messaging.Kafka.Handlers;

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