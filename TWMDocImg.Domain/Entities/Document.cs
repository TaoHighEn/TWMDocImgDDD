namespace TWMDocImg.Domain.Entities;

public class Document
{
    public Guid Id { get; set; } // 主鍵
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public required byte[] FileContent { get; set; }
    public DateTime UploadedAt { get; set; }
}