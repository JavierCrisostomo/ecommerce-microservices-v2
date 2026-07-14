using Payments.Application.Abstractions;

namespace Payments.Infrastructure.Persistence;

public class UnitOfWork(PaymentsDbContext dbContext) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => dbContext.SaveChangesAsync(cancellationToken);
}
