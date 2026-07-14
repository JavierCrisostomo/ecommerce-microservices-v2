namespace Identity.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public string Role { get; private set; } = "Customer";
    public DateTimeOffset CreatedAt { get; private set; }

    private User()
    {
    }

    public static User Create(string email, string passwordHash, string role = "Customer")
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("El email es obligatorio.", nameof(email));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("El hash de contraseña es obligatorio.", nameof(passwordHash));

        return new User
        {
            Id = Guid.NewGuid(),
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            Role = role,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
