using Identity.Domain.Entities;

namespace Identity.Domain.Repositories;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    Task AddAsync(User user, CancellationToken cancellationToken);
}
