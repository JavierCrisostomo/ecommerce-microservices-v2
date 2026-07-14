namespace Orders.Infrastructure.Persistence.ReadModels;

// Proyección desnormalizada para el historial de pedidos: las líneas se
// guardan como JSON para no tener que joinear nada al leer.
public class OrderReadModel
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string Status { get; set; } = default!;
    public decimal TotalAmount { get; set; }
    public string LinesJson { get; set; } = default!;
    public string? CancellationReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
