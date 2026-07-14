using FluentValidation;

namespace Orders.Application.Orders.Queries.ListOrdersByCustomer;

public class ListOrdersByCustomerQueryValidator : AbstractValidator<ListOrdersByCustomerQuery>
{
    public ListOrdersByCustomerQueryValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
