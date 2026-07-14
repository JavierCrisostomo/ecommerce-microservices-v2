namespace Orders.Domain.Entities;

public record NewOrderLine(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity);
