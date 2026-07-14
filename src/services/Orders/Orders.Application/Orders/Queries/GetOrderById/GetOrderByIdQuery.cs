using MediatR;

namespace Orders.Application.Orders.Queries.GetOrderById;

public record GetOrderByIdQuery(Guid OrderId) : IRequest<OrderSummary?>;
