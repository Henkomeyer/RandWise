using Microsoft.EntityFrameworkCore;
using RandWise.Application.Common;
using RandWise.Application.Security;
using RandWise.Application.WhatsApp;
using RandWise.Contracts.WhatsApp;
using RandWise.Domain.Entities;
using RandWise.Infrastructure.Persistence;

namespace RandWise.Infrastructure.WhatsApp;

public sealed class EfWhatsAppWebhookService : IWhatsAppWebhookService
{
    private readonly RandWiseDbContext dbContext;
    private readonly IClock clock;
    private readonly IIdGenerator idGenerator;
    private readonly ISensitiveDataProtector protector;

    public EfWhatsAppWebhookService(
        RandWiseDbContext dbContext,
        IClock clock,
        IIdGenerator idGenerator,
        ISensitiveDataProtector protector)
    {
        this.dbContext = dbContext;
        this.clock = clock;
        this.idGenerator = idGenerator;
        this.protector = protector;
    }

    public async Task<WhatsAppWebhookIngestionResponse> IngestAsync(
        WhatsAppWebhookRequest request,
        string payloadHash,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.IncomingMessages
            .AsNoTracking()
            .SingleOrDefaultAsync(message => message.WhatsAppMessageId == request.MessageId, cancellationToken);

        if (existing is not null)
        {
            return new WhatsAppWebhookIngestionResponse(
                request.MessageId,
                true,
                true,
                existing.ProcessingStatus.ToString());
        }

        var userId = await ResolveUserIdAsync(request, cancellationToken);
        var receivedUtc = request.ReceivedUtc is { Kind: DateTimeKind.Utc } utc
            ? utc
            : clock.UtcNow;

        var incoming = IncomingMessage.Create(
            idGenerator.NewId(),
            userId,
            request.MessageId,
            request.PlatformContactId,
            string.IsNullOrWhiteSpace(request.MessageType) ? "text" : request.MessageType,
            string.IsNullOrWhiteSpace(request.Text) ? null : protector.Protect(request.Text),
            payloadHash,
            receivedUtc);

        dbContext.IncomingMessages.Add(incoming);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            if (await IsDuplicateAsync(request.MessageId, cancellationToken))
            {
                return new WhatsAppWebhookIngestionResponse(request.MessageId, true, true, "IgnoredDuplicate");
            }

            throw;
        }

        return new WhatsAppWebhookIngestionResponse(
            request.MessageId,
            true,
            false,
            incoming.ProcessingStatus.ToString());
    }

    private async Task<string?> ResolveUserIdAsync(WhatsAppWebhookRequest request, CancellationToken cancellationToken)
    {
        var contact = await dbContext.WhatsAppContacts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                contact => contact.PlatformContactId == request.PlatformContactId,
                cancellationToken);

        if (contact is not null)
        {
            return contact.UserId;
        }

        if (!string.IsNullOrWhiteSpace(request.FromPhoneNumber))
        {
            var phoneHash = EfWhatsAppContactService.Hash(EfWhatsAppContactService.NormalizePhoneNumber(request.FromPhoneNumber));
            contact = await dbContext.WhatsAppContacts
                .AsNoTracking()
                .FirstOrDefaultAsync(contact => contact.PhoneNumberHash == phoneHash, cancellationToken);
        }

        return contact?.UserId;
    }

    private async Task<bool> IsDuplicateAsync(string messageId, CancellationToken cancellationToken) =>
        await dbContext.IncomingMessages
            .AsNoTracking()
            .AnyAsync(message => message.WhatsAppMessageId == messageId, cancellationToken);
}
