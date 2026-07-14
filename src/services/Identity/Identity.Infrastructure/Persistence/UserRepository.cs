using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence;

public class UserRepository(IdentityDbContext dbContext) : IUserRepository
{
    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return dbContext.Users.SingleOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        await dbContext.Users.AddAsync(user, cancellationToken);
    }
}
