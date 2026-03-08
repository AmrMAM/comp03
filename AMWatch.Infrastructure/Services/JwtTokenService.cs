using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AMWatch.Application.Interfaces;
using AMWatch.Domain.Entities;
using Microsoft.IdentityModel.Tokens;

namespace AMWatch.Infrastructure.Services;

public class JwtTokenService : IJwtTokenGenerator
{
    private readonly string _secret;

    public JwtTokenService(string secret = "dev-only-secret-key-change-in-production")
    {
        _secret = secret;
    }

    public string Generate(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
