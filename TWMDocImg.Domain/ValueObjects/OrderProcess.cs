namespace TWMDocImg.Domain.ValueObjects;

public record OrderProcess(string OrderId, string CustomerId, decimal TotalAmount, string Status, DateTime OrderDate); 