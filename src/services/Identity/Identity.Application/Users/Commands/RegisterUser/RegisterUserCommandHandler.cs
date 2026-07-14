using Identity.Application.Abstractions;
using Identity.Application.Exceptions;
using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using MediatR;

namespace Identity.Application.Users.Commands.RegisterUser;

public class RegisterUserCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork) : IRequestHandler<RegisterUserCommand, RegisterUserResult>
{
    public async Task<RegisterUserResult> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var existingUser = await userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUser is not null)
            throw new EmailAlreadyInUseException(request.Email);

        var passwordHash = passwordHasher.Hash(request.Password);
        var user = User.Create(request.Email, passwordHash);

        await userRepository.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RegisterUserResult(user.Id, user.Email);
    }
}
