using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Rivhit.Api.Data;

namespace Rivhit.Api.Auth;

public sealed class JwtTokenService(IOptions<JwtOptions> jwtOptions)
{
    public (string AccessToken, DateTimeOffset ExpiresAtUtc) CreateAccessToken(
        ApplicationUser user,
        IReadOnlyList<string> roles)
    {
        var opts = jwtOptions.Value;
        if (string.IsNullOrWhiteSpace(opts.Issuer) ||
            string.IsNullOrWhiteSpace(opts.Audience) ||
            string.IsNullOrWhiteSpace(opts.SigningKey))
        {
            throw new InvalidOperationException("JWT options are not configured.");
        }

        var now = DateTimeOffset.UtcNow;
        var expires = now.AddMinutes(Math.Max(5, opts.ExpiryMinutes));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opts.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: opts.Issuer,
            audience: opts.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: creds
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}

