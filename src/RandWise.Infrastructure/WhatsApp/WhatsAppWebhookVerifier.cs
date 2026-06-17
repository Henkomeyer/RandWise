using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using RandWise.Application.WhatsApp;

namespace RandWise.Infrastructure.WhatsApp;

public sealed class WhatsAppWebhookVerifier : IWhatsAppWebhookVerifier
{
    private readonly WhatsAppOptions options;

    public WhatsAppWebhookVerifier(IOptions<WhatsAppOptions> options)
    {
        this.options = options.Value;
    }

    public bool VerifyChallenge(string mode, string verifyToken) =>
        string.Equals(mode, "subscribe", StringComparison.Ordinal)
        && string.Equals(verifyToken, options.VerifyToken, StringComparison.Ordinal);

    public bool VerifyPayload(string rawBody, string? signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(options.AppSecret))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(signatureHeader) || !signatureHeader.StartsWith("sha256=", StringComparison.Ordinal))
        {
            return false;
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(options.AppSecret));
        var expected = $"sha256={Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody))).ToLowerInvariant()}";
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signatureHeader));
    }
}
