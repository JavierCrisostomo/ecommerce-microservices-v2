using Orders.Application.Abstractions;
using MediatR;

namespace Orders.Application.Orders.Queries.ListOrdersByCustomer;

public class ListOrdersByCustomerQueryHandler(IOrderReadStore orderReadStore)
    : IRequestHandler<ListOrdersByCustomerQuery, IReadOnlyList<OrderSummary>>
{
    public Task<IReadOnlyList<OrderSummary>> Handle(ListOrdersByCustomerQuery request, CancellationToken cancellationToken)
        => orderReadStore.ListByCustomerAsync(request.CustomerId, request.Page, request.PageSize, cancellationToken);
}
