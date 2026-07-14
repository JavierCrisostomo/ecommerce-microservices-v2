using MediatR;
using Payments.Domain.Repositories;

namespace Payments.Application.Payments.Queries.GetPaymentByOrderId;

public class GetPaymentByOrderIdQueryHandler(IPaymentRepository paymentRepository)
    : IRequestHandler<GetPaymentByOrderIdQuery, PaymentSummary?>
{
    public async Task<PaymentSummary?> Handle(GetPaymentByOrderIdQuery request, CancellationToken cancellationToken)
    {
        var payment = await paymentRepository.GetByOrderIdAsync(request.OrderId, cancellationToken);
        if (payment is null)
            return null;

        return new PaymentSummary(payment.Id, payment.OrderId, payment.Amount, payment.Status.ToString(), payment.FailureReason, payment.CreatedAt);
    }
}
