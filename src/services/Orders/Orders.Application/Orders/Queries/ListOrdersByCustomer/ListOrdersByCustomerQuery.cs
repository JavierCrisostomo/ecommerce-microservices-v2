using MediatR;

namespace Orders.Application.Orders.Queries.ListOrdersByCustomer;

public record ListOrdersByCustomerQuery(Guid CustomerId, int Page = 1, int PageSize = 20) : IRequest<IReadOnlyList<OrderSummary>>;
