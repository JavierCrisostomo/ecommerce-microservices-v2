using FluentAssertions;
using Identity.Application.Abstractions;
using Identity.Application.Exceptions;
using Identity.Application.Users.Commands.Login;
using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Moq;

namespace Identity.Tests.Application.Users.Commands.Login;

public class LoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGenerator = new();
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _handler = new LoginCommandHandler(_userRepository.Object, _passwordHasher.Object, _jwtTokenGenerator.Object);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsAccessToken()
    {
        var user = User.Create("ada@lovelace.dev", "hashed-password");
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);

        _userRepository.Setup(r => r.GetByEmailAsync("ada@lovelace.dev", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify("hashed-password", "SuperSecret123")).Returns(true);
        _jwtTokenGenerator.Setup(g => g.Generate(user)).Returns(new AccessToken("jwt-token", expiresAt));

        var result = await _handler.Handle(new LoginCommand("ada@lovelace.dev", "SuperSecret123"), CancellationToken.None);

        result.AccessToken.Should().Be("jwt-token");
        result.ExpiresAt.Should().Be(expiresAt);
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ThrowsInvalidCredentials()
    {
        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var act = () => _handler.Handle(new LoginCommand("ghost@example.com", "whatever"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task Handle_WhenPasswordIsWrong_ThrowsInvalidCredentials()
    {
        var user = User.Create("ada@lovelace.dev", "hashed-password");
        _userRepository.Setup(r => r.GetByEmailAsync("ada@lovelace.dev", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify("hashed-password", "WrongPassword")).Returns(false);

        var act = () => _handler.Handle(new LoginCommand("ada@lovelace.dev", "WrongPassword"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCredentialsException>();
        _jwtTokenGenerator.Verify(g => g.Generate(It.IsAny<User>()), Times.Never);
    }
}
