using FluentAssertions;
using Identity.Application.Abstractions;
using Identity.Application.Exceptions;
using Identity.Application.Users.Commands.RegisterUser;
using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Moq;

namespace Identity.Tests.Application.Users.Commands.RegisterUser;

public class RegisterUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly RegisterUserCommandHandler _handler;

    public RegisterUserCommandHandlerTests()
    {
        _handler = new RegisterUserCommandHandler(_userRepository.Object, _passwordHasher.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_WhenEmailIsFree_CreatesUserAndPersists()
    {
        _userRepository.Setup(r => r.GetByEmailAsync("ada@lovelace.dev", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _passwordHasher.Setup(h => h.Hash("SuperSecret123")).Returns("hashed-password");

        var result = await _handler.Handle(new RegisterUserCommand("ada@lovelace.dev", "SuperSecret123"), CancellationToken.None);

        result.Email.Should().Be("ada@lovelace.dev");
        result.UserId.Should().NotBeEmpty();

        _userRepository.Verify(r => r.AddAsync(
            It.Is<User>(u => u.Email == "ada@lovelace.dev" && u.PasswordHash == "hashed-password"),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyRegistered_ThrowsAndDoesNotPersist()
    {
        var existingUser = User.Create("ada@lovelace.dev", "existing-hash");
        _userRepository.Setup(r => r.GetByEmailAsync("ada@lovelace.dev", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        var act = () => _handler.Handle(new RegisterUserCommand("ada@lovelace.dev", "SuperSecret123"), CancellationToken.None);

        await act.Should().ThrowAsync<EmailAlreadyInUseException>();
        _userRepository.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
