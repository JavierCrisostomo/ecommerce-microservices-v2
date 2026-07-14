using Notifications.Domain.Entities;
using Notifications.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Notifications.Infrastructure.Persistence;

public class NotificationRepository(NotificationsDbContext dbContext) : INotificationRepository
{
    public Task<List<Notification>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken)
        => dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.OrderId == orderId)
            .OrderBy(n => n.SentAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Notification notification, CancellationToken cancellationToken)
    {
        await dbContext.Notifications.AddAsync(notification, cancellationToken);
    }
}
