namespace TWMDocImg.Domain.DomainEvents;

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