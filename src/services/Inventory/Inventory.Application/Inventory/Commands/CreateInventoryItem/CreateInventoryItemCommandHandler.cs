using Inventory.Application.Abstractions;
using Inventory.Application.Exceptions;
using Inventory.Domain.Entities;
using Inventory.Domain.Repositories;
using MediatR;

namespace Inventory.Application.Inventory.Commands.CreateInventoryItem;

public class CreateInventoryItemCommandHandler(
    IInventoryItemRepository inventoryItemRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateInventoryItemCommand, CreateInventoryItemResult>
{
    public async Task<CreateInventoryItemResult> Handle(CreateInventoryItemCommand request, CancellationToken cancellationToken)
    {
        var existing = await inventoryItemRepository.GetByProductIdAsync(request.ProductId, cancellationToken);
        if (existing is not null)
            throw new DuplicateInventoryItemException(request.ProductId);

        var item = InventoryItem.Create(request.ProductId, request.InitialQuantity);

        await inventoryItemRepository.AddAsync(item, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateInventoryItemResult(item.ProductId, item.AvailableQuantity);
    }
}
