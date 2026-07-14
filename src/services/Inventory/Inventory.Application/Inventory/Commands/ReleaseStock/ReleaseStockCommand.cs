using MediatR;

namespace Inventory.Application.Inventory.Commands.ReleaseStock;

public record ReleaseStockCommand(Guid OrderId) : IRequest;
