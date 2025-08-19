using TWMDocImg.Application.DTOs;

namespace TWMDocImg.Application.Interfaces;

public interface IFileQueueService
{
	Task QueueFileForProcessingAsync(DocumentUploadDto document);
} 