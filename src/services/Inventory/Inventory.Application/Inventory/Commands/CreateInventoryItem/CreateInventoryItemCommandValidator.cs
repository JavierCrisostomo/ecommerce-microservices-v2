using FluentValidation;

namespace Inventory.Application.Inventory.Commands.CreateInventoryItem;

public class CreateInventoryItemCommandValidator : AbstractValidator<CreateInventoryItemCommand>
{
    public CreateInventoryItemCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.InitialQuantity).GreaterThanOrEqualTo(0);
    }
}
