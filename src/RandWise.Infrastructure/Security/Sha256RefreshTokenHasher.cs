using System.Security.Cryptography;
using System.Text;
using RandWise.Application.Security;

namespace RandWise.Infrastructure.Security;

public sealed class Sha256RefreshTokenHasher : IRefreshTokenHasher
{
    private const string Prefix = "sha256:";

    public string Hash(string refreshToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

        var tokenBytes = Encoding.UTF8.GetBytes(refreshToken);
        var hashBytes = SHA256.HashData(tokenBytes);

        return Prefix + Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public bool Verify(string refreshToken, string tokenHash)
    {
        if (string.IsNullOrWhiteSpace(refreshToken) || string.IsNullOrWhiteSpace(tokenHash))
        {
            return false;
        }

        var computedHash = Hash(refreshToken);
        var computedBytes = Encoding.UTF8.GetBytes(computedHash);
        var expectedBytes = Encoding.UTF8.GetBytes(tokenHash);

        return computedBytes.Length == expectedBytes.Length
            && CryptographicOperations.FixedTimeEquals(computedBytes, expectedBytes);
    }
}
