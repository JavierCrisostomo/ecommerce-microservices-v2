using Payments.Domain.Entities;

namespace Payments.Domain.Repositories;

public interface IPaymentRepository
{
    Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);

    Task AddAsync(Payment payment, CancellationToken cancellationToken);
}
