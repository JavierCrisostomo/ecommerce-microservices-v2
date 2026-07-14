using MediatR;

namespace Orders.Application.Orders.Commands.CreateOrder;

public record CreateOrderLineRequest(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity);

public record CreateOrderCommand(
    Guid CustomerId,
    IReadOnlyCollection<CreateOrderLineRequest> Lines) : IRequest<CreateOrderResult>;

public record CreateOrderResult(Guid OrderId, decimal TotalAmount, string Status);
