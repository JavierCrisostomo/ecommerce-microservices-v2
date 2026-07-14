using Orders.Application.Abstractions;
using MediatR;

namespace Orders.Application.Orders.Queries.GetOrderById;

public class GetOrderByIdQueryHandler(IOrderReadStore orderReadStore)
    : IRequestHandler<GetOrderByIdQuery, OrderSummary?>
{
    public Task<OrderSummary?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
        => orderReadStore.GetByIdAsync(request.OrderId, cancellationToken);
}
