using Microsoft.AspNetCore.Mvc;
using TWMDocImg.Application.Interfaces;
using TWMDocImg.Application.DTOs;
using TWMDocImg.Application.Configurations;
using Microsoft.Extensions.Options;

namespace TWMDocImg.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly ILogger<DocumentsController> _logger;
    private readonly IFileQueueService _fileQueueService;
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB
    private readonly FileUploadOptions _options;

    public DocumentsController(ILogger<DocumentsController> logger, IOptions<FileUploadOptions> options, IFileQueueService fileQueueService)
    {
        _logger = logger;
        _fileQueueService = fileQueueService;
        _options = options.Value;
    }

    [HttpPost("upload")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadDocument(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("未上傳任何檔案。");
        }

        // 檢查檔案大小
        if (file.Length > MaxFileSize)
        {
            return BadRequest($"檔案大小超過限制 ({MaxFileSize / 1024 / 1024} MB)。");
        }

        // 檢查副檔名
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(fileExtension) || !_options.AllowedExtensions.Contains(fileExtension))
        {
            return BadRequest($"不支援的檔案格式。僅允許: {string.Join(", ", _options.AllowedExtensions)}");
        }

        try
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();

            var documentDto = new DocumentUploadDto
            {
                FileId = Guid.NewGuid(),
                FileName = file.FileName,
                ContentType = file.ContentType,
                FileContent = fileBytes
            };

            // 將檔案資訊發送到 Kafka
            await _fileQueueService.QueueFileForProcessingAsync(documentDto);

            _logger.LogInformation("檔案 {FileName} 已成功接收並發送到佇列，FileId: {FileId}", file.FileName, documentDto.FileId);

            // 回傳 202 Accepted 表示伺服器已接受請求，但仍在處理中
            return Accepted(new { Message = "上傳成功，等待檔案寫入。", FileId = documentDto.FileId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "上傳檔案 {FileName} 時發生錯誤。", file.FileName);
            return StatusCode(StatusCodes.Status500InternalServerError, "伺服器內部錯誤，上傳失敗。");
        }
    }
}

