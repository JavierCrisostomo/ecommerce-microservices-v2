using MediatR;

namespace Identity.Application.Users.Commands.Login;

public record LoginCommand(string Email, string Password) : IRequest<LoginResult>;

public record LoginResult(string AccessToken, DateTimeOffset ExpiresAt);
