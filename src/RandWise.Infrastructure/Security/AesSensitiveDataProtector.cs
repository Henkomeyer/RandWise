using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using RandWise.Application.Security;

namespace RandWise.Infrastructure.Security;

public sealed class AesSensitiveDataProtector : ISensitiveDataProtector
{
    private readonly byte[] key;

    public AesSensitiveDataProtector(IOptions<SensitiveDataOptions> options, IOptions<JwtTokenOptions> jwtOptions)
    {
        var configuredKey = !string.IsNullOrWhiteSpace(options.Value.Key)
            ? options.Value.Key
            : jwtOptions.Value.SigningKey;

        key = SHA256.HashData(Encoding.UTF8.GetBytes(configuredKey));
    }

    public string Protect(string plaintext)
    {
        var nonce = RandomNumberGenerator.GetBytes(AesGcm.NonceByteSizes.MaxSize);
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];

        using var aes = new AesGcm(key, tag.Length);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        return Convert.ToBase64String(nonce.Concat(tag).Concat(ciphertext).ToArray());
    }
}
