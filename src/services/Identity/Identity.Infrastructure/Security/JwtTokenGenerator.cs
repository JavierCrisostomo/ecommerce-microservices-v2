using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Identity.Application.Abstractions;
using Identity.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Identity.Infrastructure.Security;

public class JwtTokenGenerator(IOptions<JwtOptions> options) : IJwtTokenGenerator
{
    private readonly JwtOptions _options = options.Value;

    public AccessToken Generate(User user)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_options.ExpirationMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        var value = new JwtSecurityTokenHandler().WriteToken(token);
        return new AccessToken(value, expiresAt);
    }
}
