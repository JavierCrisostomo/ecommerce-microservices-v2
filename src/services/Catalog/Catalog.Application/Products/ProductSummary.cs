namespace Catalog.Application.Products;

public record ProductSummary(
    Guid Id,
    string Sku,
    string Name,
    string Category,
    decimal Price,
    DateTimeOffset CreatedAt);
