namespace Catalog.Domain.Entities;

public class Product
{
    public Guid Id { get; private set; }
    public string Sku { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public string Category { get; private set; } = default!;
    public decimal Price { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Product()
    {
    }

    public static Product Create(string sku, string name, string description, string category, decimal price)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("El SKU es obligatorio.", nameof(sku));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre es obligatorio.", nameof(name));

        if (price <= 0)
            throw new ArgumentOutOfRangeException(nameof(price), "El precio debe ser mayor a cero.");

        return new Product
        {
            Id = Guid.NewGuid(),
            Sku = sku.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Description = description.Trim(),
            Category = category.Trim(),
            Price = price,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
