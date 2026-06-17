using RandWise.Contracts.WhatsApp;

namespace RandWise.Application.WhatsApp;

public interface IWhatsAppContactService
{
    Task<WhatsAppStatusResponse> GetStatusAsync(string userId, CancellationToken cancellationToken);

    Task<WhatsAppStatusResponse> LinkAsync(
        string userId,
        LinkWhatsAppRequest request,
        CancellationToken cancellationToken);

    Task UnlinkAsync(string userId, CancellationToken cancellationToken);
}
