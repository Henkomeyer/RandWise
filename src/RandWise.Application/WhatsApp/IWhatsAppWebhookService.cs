using RandWise.Contracts.WhatsApp;

namespace RandWise.Application.WhatsApp;

public interface IWhatsAppWebhookService
{
    Task<WhatsAppWebhookIngestionResponse> IngestAsync(
        WhatsAppWebhookRequest request,
        string payloadHash,
        CancellationToken cancellationToken);
}
