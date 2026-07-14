using FluentValidation;

namespace Payments.Application.Payments.Commands.ProcessPayment;

public class ProcessPaymentCommandValidator : AbstractValidator<ProcessPaymentCommand>
{
    public ProcessPaymentCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}
