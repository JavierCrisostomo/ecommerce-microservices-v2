using MediatR;

namespace Payments.Application.Payments.Queries.GetPaymentByOrderId;

public record GetPaymentByOrderIdQuery(Guid OrderId) : IRequest<PaymentSummary?>;
