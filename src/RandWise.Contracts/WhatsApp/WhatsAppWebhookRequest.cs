namespace RandWise.Contracts.WhatsApp;

public sealed record WhatsAppWebhookRequest(
    string MessageId,
    string PlatformContactId,
    string? FromPhoneNumber,
    string MessageType,
    string? Text,
    DateTime? ReceivedUtc);
