using Microsoft.Extensions.Logging;
using TWMDocImg.Application.DTOs;
using TWMDocImg.Application.Interfaces;
using TWMDocImg.Domain.Entities;
using TWMDocImg.Domain.Services;

namespace TWMDocImg.Infrastructure.Persistence;

public class DocumentStorageService : IDocumentStorageService
{
	private readonly AppDbContext _context;
	private readonly ILogger<DocumentStorageService> _logger;
	private readonly IDomainEventBus _domainEventBus;

	public DocumentStorageService(AppDbContext context, ILogger<DocumentStorageService> logger, IDomainEventBus domainEventBus)
	{
		_context = context;
		_logger = logger;
		_domainEventBus = domainEventBus;
	}

	public async Task SaveDocumentAsync(DocumentUploadDto documentDto)
	{
		try
		{
			var document = new Document
			{
				Id = documentDto.FileId,
				FileName = documentDto.FileName,
				ContentType = documentDto.ContentType,
				FileContent = documentDto.FileContent,
				UploadedAt = DateTime.UtcNow
			};

			//await _context.Documents.AddAsync(document);
			//await _context.SaveChangesAsync();

			_logger.LogInformation("文件 {FileId} 已成功儲存到資料庫。", document.Id);

			// 發佈領域事件：這裡示範建立一則通知事件
			var notificationEvent = new TWMDocImg.Domain.DomainEvents.NotificationCreatedEvent(
				userId: "system",
				title: $"文件已儲存: {document.FileName}");
			await _domainEventBus.PublishAsync(notificationEvent);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "儲存文件 {FileId} 到資料庫時失敗。", documentDto.FileId);
			throw;
		}
	}
}