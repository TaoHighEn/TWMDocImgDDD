using TWMDocImg.Application.DTOs;

namespace TWMDocImg.Application.Interfaces;

public interface IDocumentStorageService
{
	Task SaveDocumentAsync(DocumentUploadDto documentDto);
} 