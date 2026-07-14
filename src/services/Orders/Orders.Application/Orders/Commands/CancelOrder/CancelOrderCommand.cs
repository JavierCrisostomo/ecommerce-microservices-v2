using MediatR;

namespace Orders.Application.Orders.Commands.CancelOrder;

public record CancelOrderCommand(Guid OrderId, string Reason) : IRequest;
