using Orders.Application.Orders;

namespace Orders.Application.Abstractions;

public interface IOrderReadStore
{
    Task UpsertAsync(OrderSummary summary, CancellationToken cancellationToken);

    Task<OrderSummary?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken);

    Task<IReadOnlyList<OrderSummary>> ListByCustomerAsync(
        Guid customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
