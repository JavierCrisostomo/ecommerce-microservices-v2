using Identity.Application.Abstractions;
using Identity.Application.Exceptions;
using Identity.Domain.Repositories;
using MediatR;

namespace Identity.Application.Users.Commands.Login;

public class LoginCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator) : IRequestHandler<LoginCommand, LoginResult>
{
    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null || !passwordHasher.Verify(user.PasswordHash, request.Password))
            throw new InvalidCredentialsException();

        var token = jwtTokenGenerator.Generate(user);
        return new LoginResult(token.Value, token.ExpiresAt);
    }
}
