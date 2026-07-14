using FluentValidation.TestHelper;
using Identity.Application.Users.Commands.Login;

namespace Identity.Tests.Application.Users.Commands.Login;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_HasNoErrors()
    {
        var result = _validator.TestValidate(new LoginCommand("ada@lovelace.dev", "whatever"));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithInvalidEmail_HasError()
    {
        var result = _validator.TestValidate(new LoginCommand("not-an-email", "whatever"));

        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public void Validate_WithEmptyPassword_HasError()
    {
        var result = _validator.TestValidate(new LoginCommand("ada@lovelace.dev", ""));

        result.ShouldHaveValidationErrorFor(c => c.Password);
    }
}
