using FluentValidation;

namespace Orders.Application.Orders.Commands.CreateOrder;

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();

        RuleFor(x => x.Lines)
            .NotEmpty()
            .WithMessage("El pedido debe tener al menos una línea.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).NotEmpty();
            line.RuleFor(l => l.ProductName).NotEmpty().MaximumLength(200);
            line.RuleFor(l => l.UnitPrice).GreaterThan(0);
            line.RuleFor(l => l.Quantity).GreaterThan(0);
        });
    }
}
