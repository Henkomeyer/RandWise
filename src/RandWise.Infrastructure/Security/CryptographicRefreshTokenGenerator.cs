using System.Security.Cryptography;
using RandWise.Application.Security;

namespace RandWise.Infrastructure.Security;

public sealed class CryptographicRefreshTokenGenerator : IRefreshTokenGenerator
{
    private const int TokenByteLength = 64;

    public string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(TokenByteLength);

        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
