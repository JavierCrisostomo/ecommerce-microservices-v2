using System.Globalization;
using Payments.Application.Abstractions;

namespace Payments.Infrastructure.Gateways;

// Stub de una pasarela real (Stripe en modo test, por ejemplo). Simula un
// rechazo para montos grandes únicamente para poder probar el camino de
// falla de la saga sin depender de un proveedor externo.
public class SimulatedPaymentGateway : IPaymentGateway
{
    private const decimal RejectionThreshold = 1000m;

    public PaymentGatewayResult Charge(Guid orderId, decimal amount)
    {
        if (amount > RejectionThreshold)
            return new PaymentGatewayResult(false, $"Pago rechazado: el monto {amount.ToString("N2", CultureInfo.InvariantCulture)} supera el límite permitido.");

        return new PaymentGatewayResult(true, null);
    }
}
