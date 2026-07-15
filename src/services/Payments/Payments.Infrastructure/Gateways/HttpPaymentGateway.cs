using System.Net.Http.Json;
using Payments.Application.Abstractions;

namespace Payments.Infrastructure.Gateways;

public class HttpPaymentGateway(HttpClient httpClient) : IPaymentGateway
{
    public async Task<PaymentGatewayResult> ChargeAsync(Guid orderId, decimal amount, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync(
            "/internal/payment-gateway/charge",
            new GatewayChargeRequest(orderId, amount),
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<GatewayChargeResponse>(cancellationToken);
        return new PaymentGatewayResult(result!.Success, result.FailureReason);
    }
}
