using System.Globalization;
using Payments.Application.Abstractions;

namespace Payments.Infrastructure.Gateways;

// Lógica de "el banco": la invoca el endpoint interno que simula la pasarela
// externa (ver /internal/payment-gateway/charge en Payments.Api), no el
// cliente HTTP. Simula un rechazo para montos grandes únicamente para poder
// probar el camino de falla de la saga sin depender de un proveedor externo.
public static class SimulatedGatewayBackend
{
    private const decimal RejectionThreshold = 1000m;

    public static PaymentGatewayResult Charge(Guid orderId, decimal amount)
    {
        if (amount > RejectionThreshold)
            return new PaymentGatewayResult(false, $"Pago rechazado: el monto {amount.ToString("N2", CultureInfo.InvariantCulture)} supera el límite permitido.");

        return new PaymentGatewayResult(true, null);
    }
}
