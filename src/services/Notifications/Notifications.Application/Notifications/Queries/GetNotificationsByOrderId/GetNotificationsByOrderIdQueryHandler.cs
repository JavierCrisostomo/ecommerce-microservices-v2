using MediatR;
using Notifications.Domain.Repositories;

namespace Notifications.Application.Notifications.Queries.GetNotificationsByOrderId;

public class GetNotificationsByOrderIdQueryHandler(INotificationRepository notificationRepository)
    : IRequestHandler<GetNotificationsByOrderIdQuery, IReadOnlyList<NotificationSummary>>
{
    public async Task<IReadOnlyList<NotificationSummary>> Handle(GetNotificationsByOrderIdQuery request, CancellationToken cancellationToken)
    {
        var notifications = await notificationRepository.GetByOrderIdAsync(request.OrderId, cancellationToken);

        return notifications
            .Select(n => new NotificationSummary(n.Id, n.OrderId, n.CustomerId, n.Type.ToString(), n.Message, n.SentAt))
            .ToList();
    }
}
