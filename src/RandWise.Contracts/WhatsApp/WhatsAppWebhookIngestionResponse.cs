namespace RandWise.Contracts.WhatsApp;

public sealed record WhatsAppWebhookIngestionResponse(
    string MessageId,
    bool Accepted,
    bool Duplicate,
    string ProcessingStatus);
