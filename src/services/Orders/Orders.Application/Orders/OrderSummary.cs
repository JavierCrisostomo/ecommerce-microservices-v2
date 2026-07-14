namespace Orders.Application.Orders;

public record OrderLineSummary(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity, decimal LineTotal);

public record OrderSummary(
    Guid Id,
    Guid CustomerId,
    string Status,
    decimal TotalAmount,
    IReadOnlyCollection<OrderLineSummary> Lines,
    DateTimeOffset CreatedAt,
    string? CancellationReason = null);
