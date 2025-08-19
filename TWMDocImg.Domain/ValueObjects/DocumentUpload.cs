namespace TWMDocImg.Domain.ValueObjects;

public record DocumentUpload(string FileName, string ContentType, byte[] FileContent); 