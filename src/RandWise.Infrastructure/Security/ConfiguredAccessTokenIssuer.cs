using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RandWise.Application.Security;

namespace RandWise.Infrastructure.Security;

public sealed class ConfiguredAccessTokenIssuer : IAccessTokenIssuer
{
    private readonly JwtTokenOptions options;

    public ConfiguredAccessTokenIssuer(IOptions<JwtTokenOptions> options)
    {
        this.options = options.Value;
    }

    public string IssueAccessToken(AccessTokenDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (string.IsNullOrWhiteSpace(options.SigningKey))
        {
            throw new InvalidOperationException("JWT signing key is not configured.");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, descriptor.UserId),
            new Claim(JwtRegisteredClaimNames.Email, descriptor.Email),
            new Claim(ClaimTypes.NameIdentifier, descriptor.UserId),
            new Claim(ClaimTypes.Email, descriptor.Email),
            new Claim(ClaimTypes.Name, descriptor.DisplayName),
            new Claim("app_user_id", descriptor.UserId)
        };

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            notBefore: descriptor.IssuedUtc.UtcDateTime,
            expires: descriptor.ExpiresUtc.UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
