using RandWise.Application.WhatsApp;

namespace RandWise.Infrastructure.WhatsApp;

public sealed class NoOpWhatsAppOutboundClient : IWhatsAppOutboundClient
{
    public Task SendTextAsync(string platformContactId, string message, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(platformContactId);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return Task.CompletedTask;
    }
}
