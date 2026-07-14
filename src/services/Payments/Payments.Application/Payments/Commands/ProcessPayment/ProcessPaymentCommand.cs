using MediatR;

namespace Payments.Application.Payments.Commands.ProcessPayment;

public record ProcessPaymentCommand(Guid OrderId, decimal Amount) : IRequest;
