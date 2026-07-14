using Orders.Domain.Entities;

namespace Orders.Application.Orders;

public static class OrderSummaryMapper
{
    public static OrderSummary ToSummary(Order order) => new(
        order.Id,
        order.CustomerId,
        order.Status.ToString(),
        order.TotalAmount,
        order.Lines.Select(l => new OrderLineSummary(l.ProductId, l.ProductName, l.UnitPrice, l.Quantity, l.LineTotal)).ToList(),
        order.CreatedAt,
        order.CancellationReason);
}
