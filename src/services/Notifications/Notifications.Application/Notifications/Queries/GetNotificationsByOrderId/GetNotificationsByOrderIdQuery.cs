using MediatR;

namespace Notifications.Application.Notifications.Queries.GetNotificationsByOrderId;

public record GetNotificationsByOrderIdQuery(Guid OrderId) : IRequest<IReadOnlyList<NotificationSummary>>;
