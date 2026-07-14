using MediatR;
using Microsoft.Extensions.Logging;
using Notifications.Application.Abstractions;
using Notifications.Domain.Entities;
using Notifications.Domain.Repositories;

namespace Notifications.Application.Notifications.Commands.SendNotification;

public class SendNotificationCommandHandler(
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork,
    ILogger<SendNotificationCommandHandler> logger) : IRequestHandler<SendNotificationCommand>
{
    public async Task Handle(SendNotificationCommand request, CancellationToken cancellationToken)
    {
        // Stub de un proveedor real (SendGrid, SES, etc.): "enviar" acá es
        // loguear + dejar constancia en la bitácora de notificaciones.
        logger.LogInformation(
            "Email simulado -> cliente {CustomerId} (pedido {OrderId}): {Message}",
            request.CustomerId,
            request.OrderId,
            request.Message);

        var notification = Notification.Create(request.OrderId, request.CustomerId, request.Type, request.Message);

        await notificationRepository.AddAsync(notification, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
