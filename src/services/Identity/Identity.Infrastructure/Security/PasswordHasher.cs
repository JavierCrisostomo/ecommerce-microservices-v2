using Identity.Application.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace Identity.Infrastructure.Security;

public class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(default!, password);

    public bool Verify(string hash, string password)
        => _hasher.VerifyHashedPassword(default!, hash, password) != PasswordVerificationResult.Failed;
}
