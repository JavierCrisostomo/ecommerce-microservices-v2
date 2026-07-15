using ECommerce.Contracts.IntegrationEvents;
using MediatR;
using Payments.Application.Abstractions;
using Payments.Domain.Entities;
using Payments.Domain.Repositories;

namespace Payments.Application.Payments.Commands.ProcessPayment;

public class ProcessPaymentCommandHandler(
    IPaymentRepository paymentRepository,
    IPaymentGateway paymentGateway,
    IUnitOfWork unitOfWork,
    IEventPublisher eventPublisher) : IRequestHandler<ProcessPaymentCommand>
{
    public async Task Handle(ProcessPaymentCommand request, CancellationToken cancellationToken)
    {
        // Idempotencia: StockReserved puede entregarse más de una vez.
        var existingPayment = await paymentRepository.GetByOrderIdAsync(request.OrderId, cancellationToken);
        if (existingPayment is not null)
            return;

        var payment = Payment.Create(request.OrderId, request.Amount);
        var result = paymentGateway.Charge(request.OrderId, request.Amount);

        if (result.Success)
        {
            payment.Complete();
            await paymentRepository.AddAsync(payment, cancellationToken);

            await eventPublisher.PublishAsync(
                new PaymentCompleted(Guid.NewGuid(), DateTimeOffset.UtcNow, payment.OrderId, payment.Id, payment.Amount),
                cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        else
        {
            payment.Fail(result.FailureReason ?? "El pago fue rechazado.");
            await paymentRepository.AddAsync(payment, cancellationToken);

            await eventPublisher.PublishAsync(
                new PaymentFailed(Guid.NewGuid(), DateTimeOffset.UtcNow, payment.OrderId, payment.FailureReason!),
                cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
