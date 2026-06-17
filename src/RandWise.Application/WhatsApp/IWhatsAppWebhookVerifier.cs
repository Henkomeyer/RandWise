namespace RandWise.Application.WhatsApp;

public interface IWhatsAppWebhookVerifier
{
    bool VerifyChallenge(string mode, string verifyToken);

    bool VerifyPayload(string rawBody, string? signatureHeader);
}
