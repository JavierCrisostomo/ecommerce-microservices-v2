using MediatR;
using Notifications.Domain.Entities;

namespace Notifications.Application.Notifications.Commands.SendNotification;

public record SendNotificationCommand(
    Guid OrderId,
    Guid CustomerId,
    NotificationType Type,
    string Message) : IRequest;
