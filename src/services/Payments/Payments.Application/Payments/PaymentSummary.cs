namespace Payments.Application.Payments;

public record PaymentSummary(
    Guid Id,
    Guid OrderId,
    decimal Amount,
    string Status,
    string? FailureReason,
    DateTimeOffset CreatedAt);
