namespace TWMDocImg.Domain.ValueObjects;

public record Notification(string UserId, string Title, string Message, string Type, DateTime CreatedAt); 