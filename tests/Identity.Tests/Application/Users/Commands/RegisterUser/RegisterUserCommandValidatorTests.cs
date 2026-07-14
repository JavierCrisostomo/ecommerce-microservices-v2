using FluentValidation.TestHelper;
using Identity.Application.Users.Commands.RegisterUser;

namespace Identity.Tests.Application.Users.Commands.RegisterUser;

public class RegisterUserCommandValidatorTests
{
    private readonly RegisterUserCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_HasNoErrors()
    {
        var result = _validator.TestValidate(new RegisterUserCommand("ada@lovelace.dev", "SuperSecret123"));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Validate_WithInvalidEmail_HasError(string email)
    {
        var result = _validator.TestValidate(new RegisterUserCommand(email, "SuperSecret123"));

        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData("short1")]
    public void Validate_WithWeakPassword_HasError(string password)
    {
        var result = _validator.TestValidate(new RegisterUserCommand("ada@lovelace.dev", password));

        result.ShouldHaveValidationErrorFor(c => c.Password);
    }
}
