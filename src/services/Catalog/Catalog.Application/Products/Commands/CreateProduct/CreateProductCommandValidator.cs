using FluentValidation;

namespace Catalog.Application.Products.Commands.CreateProduct;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotNull().MaximumLength(2000);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Price).GreaterThan(0);
    }
}
