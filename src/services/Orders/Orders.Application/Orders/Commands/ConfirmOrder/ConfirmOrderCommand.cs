using MediatR;

namespace Orders.Application.Orders.Commands.ConfirmOrder;

public record ConfirmOrderCommand(Guid OrderId) : IRequest;
