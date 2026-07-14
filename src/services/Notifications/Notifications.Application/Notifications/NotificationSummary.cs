namespace Notifications.Application.Notifications;

public record NotificationSummary(Guid Id, Guid OrderId, Guid CustomerId, string Type, string Message, DateTimeOffset SentAt);
