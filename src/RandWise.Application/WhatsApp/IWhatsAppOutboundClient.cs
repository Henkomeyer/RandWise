namespace RandWise.Application.WhatsApp;

public interface IWhatsAppOutboundClient
{
    Task SendTextAsync(string platformContactId, string message, CancellationToken cancellationToken);
}
