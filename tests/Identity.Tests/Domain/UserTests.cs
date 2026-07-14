using FluentAssertions;
using Identity.Domain.Entities;

namespace Identity.Tests.Domain;

public class UserTests
{
    [Fact]
    public void Create_WithValidData_NormalizesEmailAndSetsDefaults()
    {
        var user = User.Create("  Ada@Lovelace.DEV  ", "hashed-password");

        user.Email.Should().Be("ada@lovelace.dev");
        user.PasswordHash.Should().Be("hashed-password");
        user.Role.Should().Be("Customer");
        user.Id.Should().NotBeEmpty();
        user.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithCustomRole_UsesThatRole()
    {
        var user = User.Create("admin@example.com", "hashed-password", "Admin");

        user.Role.Should().Be("Admin");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithoutEmail_Throws(string? email)
    {
        var act = () => User.Create(email!, "hashed-password");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithoutPasswordHash_Throws(string? passwordHash)
    {
        var act = () => User.Create("ada@lovelace.dev", passwordHash!);

        act.Should().Throw<ArgumentException>();
    }
}
