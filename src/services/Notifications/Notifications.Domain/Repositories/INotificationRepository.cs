using Notifications.Domain.Entities;

namespace Notifications.Domain.Repositories;

public interface INotificationRepository
{
    Task<List<Notification>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);

    Task AddAsync(Notification notification, CancellationToken cancellationToken);
}
