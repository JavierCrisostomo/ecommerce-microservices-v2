namespace Orders.Api.Contracts;

public record CreateOrderLineDto(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity);

public record CreateOrderRequest(IReadOnlyCollection<CreateOrderLineDto> Lines);
