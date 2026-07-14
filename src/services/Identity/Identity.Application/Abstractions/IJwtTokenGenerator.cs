using Identity.Domain.Entities;

namespace Identity.Application.Abstractions;

public record AccessToken(string Value, DateTimeOffset ExpiresAt);

public interface IJwtTokenGenerator
{
    AccessToken Generate(User user);
}
