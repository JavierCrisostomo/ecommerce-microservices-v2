namespace Catalog.Infrastructure.Persistence.ReadModels;

// Proyección desnormalizada, optimizada para las queries del catálogo.
// Vive en su propia tabla, separada del modelo de escritura (Products).
public class ProductReadModel
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Category { get; set; } = default!;
    public decimal Price { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
