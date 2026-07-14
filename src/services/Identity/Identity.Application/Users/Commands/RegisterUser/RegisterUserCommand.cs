using MediatR;

namespace Identity.Application.Users.Commands.RegisterUser;

public record RegisterUserCommand(string Email, string Password) : IRequest<RegisterUserResult>;

public record RegisterUserResult(Guid UserId, string Email);
