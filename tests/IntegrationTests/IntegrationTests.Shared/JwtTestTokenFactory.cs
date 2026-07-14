using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace IntegrationTests.Shared;

// Emite un JWT con la misma forma (claims cortos, sin remapear) que emite
// Identity.Api en producción, usando la config real que cargó el host bajo
// prueba — así nunca se desincroniza del secreto/issuer/audience reales.
public static class JwtTestTokenFactory
{
    public static string Create(IConfiguration configuration, Guid userId, string email, string role = "Customer")
    {
        var jwtSection = configuration.GetSection("Jwt");

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Secret"]!));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSection["Issuer"],
            audience: jwtSection["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
