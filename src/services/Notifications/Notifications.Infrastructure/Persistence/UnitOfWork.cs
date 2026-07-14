using Notifications.Application.Abstractions;

namespace Notifications.Infrastructure.Persistence;

public class UnitOfWork(NotificationsDbContext dbContext) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => dbContext.SaveChangesAsync(cancellationToken);
}
