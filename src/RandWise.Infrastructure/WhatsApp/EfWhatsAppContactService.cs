using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RandWise.Application.Common;
using RandWise.Application.Security;
using RandWise.Application.WhatsApp;
using RandWise.Contracts.WhatsApp;
using RandWise.Domain.Entities;
using RandWise.Infrastructure.Persistence;
using AppException = RandWise.Application.Common.ApplicationException;

namespace RandWise.Infrastructure.WhatsApp;

public sealed class EfWhatsAppContactService : IWhatsAppContactService
{
    private readonly RandWiseDbContext dbContext;
    private readonly IClock clock;
    private readonly IIdGenerator idGenerator;
    private readonly ISensitiveDataProtector protector;

    public EfWhatsAppContactService(
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

    public async Task<WhatsAppStatusResponse> GetStatusAsync(string userId, CancellationToken cancellationToken)
    {
        var contact = await dbContext.WhatsAppContacts
            .AsNoTracking()
            .Where(contact => contact.UserId == userId)
            .OrderByDescending(contact => contact.UpdatedUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return contact is null
            ? new WhatsAppStatusResponse(false, false, null, null)
            : ToResponse(contact);
    }

    public async Task<WhatsAppStatusResponse> LinkAsync(
        string userId,
        LinkWhatsAppRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedPhoneNumber = NormalizePhoneNumber(request.PhoneNumber);
        var phoneHash = Hash(normalizedPhoneNumber);
        var now = clock.UtcNow;
        var existing = await dbContext.WhatsAppContacts
            .SingleOrDefaultAsync(contact => contact.PhoneNumberHash == phoneHash || contact.UserId == userId, cancellationToken);

        if (existing is not null && existing.UserId != userId)
        {
            throw new AppException(ApplicationError.Validation, "WhatsApp number is already linked.");
        }

        if (existing is null)
        {
            existing = WhatsAppContact.Create(
                idGenerator.NewId(),
                userId,
                phoneHash,
                protector.Protect(normalizedPhoneNumber),
                request.PlatformContactId,
                now);
            dbContext.WhatsAppContacts.Add(existing);
        }

        existing.MarkVerified(request.PlatformContactId, now);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(existing);
    }

    public async Task UnlinkAsync(string userId, CancellationToken cancellationToken)
    {
        var contacts = await dbContext.WhatsAppContacts
            .Where(contact => contact.UserId == userId)
            .ToListAsync(cancellationToken);

        dbContext.WhatsAppContacts.RemoveRange(contacts);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    internal static string NormalizePhoneNumber(string phoneNumber)
    {
        var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());
        if (digits.Length < 8)
        {
            throw new AppException(ApplicationError.Validation, "WhatsApp phone number is invalid.");
        }

        return digits;
    }

    internal static string Hash(string value) =>
        $"sha256:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant()}";

    private static WhatsAppStatusResponse ToResponse(WhatsAppContact contact) =>
        new(true, contact.IsVerified, contact.PlatformContactId, contact.VerifiedUtc);
}
