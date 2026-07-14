using System.Text.Json;
using Orders.Application.Abstractions;
using Orders.Application.Orders;
using Orders.Infrastructure.Persistence.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace Orders.Infrastructure.Persistence;

public class OrderReadStore(OrdersDbContext dbContext) : IOrderReadStore
{
    public async Task UpsertAsync(OrderSummary summary, CancellationToken cancellationToken)
    {
        var readModel = await dbContext.OrderReadModels
            .SingleOrDefaultAsync(o => o.Id == summary.Id, cancellationToken);

        var linesJson = JsonSerializer.Serialize(summary.Lines);

        if (readModel is null)
        {
            await dbContext.OrderReadModels.AddAsync(new OrderReadModel
            {
                Id = summary.Id,
                CustomerId = summary.CustomerId,
                Status = summary.Status,
                TotalAmount = summary.TotalAmount,
                LinesJson = linesJson,
                CancellationReason = summary.CancellationReason,
                CreatedAt = summary.CreatedAt
            }, cancellationToken);
        }
        else
        {
            readModel.CustomerId = summary.CustomerId;
            readModel.Status = summary.Status;
            readModel.TotalAmount = summary.TotalAmount;
            readModel.LinesJson = linesJson;
            readModel.CancellationReason = summary.CancellationReason;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<OrderSummary?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var readModel = await dbContext.OrderReadModels
            .AsNoTracking()
            .SingleOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        return readModel is null ? null : ToSummary(readModel);
    }

    public async Task<IReadOnlyList<OrderSummary>> ListByCustomerAsync(
        Guid customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var readModels = await dbContext.OrderReadModels
            .AsNoTracking()
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return readModels.Select(ToSummary).ToList();
    }

    private static OrderSummary ToSummary(OrderReadModel readModel)
    {
        var lines = JsonSerializer.Deserialize<List<OrderLineSummary>>(readModel.LinesJson) ?? [];
        return new OrderSummary(readModel.Id, readModel.CustomerId, readModel.Status, readModel.TotalAmount, lines, readModel.CreatedAt, readModel.CancellationReason);
    }
}
