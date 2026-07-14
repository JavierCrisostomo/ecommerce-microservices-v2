namespace Catalog.Api.Contracts;

public record CreateProductRequest(string Sku, string Name, string Description, string Category, decimal Price);
