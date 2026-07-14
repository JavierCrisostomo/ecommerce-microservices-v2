namespace Payments.Application.Abstractions;

public record PaymentGatewayResult(bool Success, string? FailureReason);

// Stub de una pasarela real (p. ej. Stripe en modo test). La implementación
// simulada vive en Infrastructure.
public interface IPaymentGateway
{
    PaymentGatewayResult Charge(Guid orderId, decimal amount);
}
