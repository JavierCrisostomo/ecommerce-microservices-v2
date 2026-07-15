namespace Payments.Application.Abstractions;

public record PaymentGatewayResult(bool Success, string? FailureReason);

// Cliente HTTP hacia una pasarela real (p. ej. Stripe en modo test). La
// implementación vive en Infrastructure y llama a un endpoint simulado.
public interface IPaymentGateway
{
    Task<PaymentGatewayResult> ChargeAsync(Guid orderId, decimal amount, CancellationToken cancellationToken);
}
