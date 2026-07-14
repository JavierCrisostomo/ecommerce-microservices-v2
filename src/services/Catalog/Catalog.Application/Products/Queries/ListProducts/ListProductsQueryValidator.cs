using FluentValidation;

namespace Catalog.Application.Products.Queries.ListProducts;

public class ListProductsQueryValidator : AbstractValidator<ListProductsQuery>
{
    public ListProductsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
