using Payments.Domain.Entities;
using Payments.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Payments.Infrastructure.Persistence;

public class PaymentRepository(PaymentsDbContext dbContext) : IPaymentRepository
{
    public Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken)
        => dbContext.Payments.SingleOrDefaultAsync(p => p.OrderId == orderId, cancellationToken);

    public async Task AddAsync(Payment payment, CancellationToken cancellationToken)
    {
        await dbContext.Payments.AddAsync(payment, cancellationToken);
    }
}
