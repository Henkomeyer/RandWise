namespace RandWise.Application.WhatsApp;

public interface IWhatsAppMessageProcessor
{
    Task ProcessAsync(string incomingMessageId, CancellationToken cancellationToken);
}
