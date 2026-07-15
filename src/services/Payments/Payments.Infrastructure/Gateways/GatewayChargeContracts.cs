namespace Payments.Infrastructure.Gateways;

public record GatewayChargeRequest(Guid OrderId, decimal Amount);

public record GatewayChargeResponse(bool Success, string? FailureReason);
