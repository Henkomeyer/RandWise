using RandWise.Domain.Common;

namespace RandWise.Domain.Entities;

public sealed class WhatsAppContact : UserOwnedAggregateRoot
{
    private WhatsAppContact(
        string id,
        string userId,
        string phoneNumberHash,
        string encryptedPhoneNumber,
        string? platformContactId,
        DateTime createdUtc)
        : base(id, userId)
    {
        PhoneNumberHash = DomainGuard.Required(phoneNumberHash, nameof(phoneNumberHash), 256);
        EncryptedPhoneNumber = DomainGuard.Required(encryptedPhoneNumber, nameof(encryptedPhoneNumber));
        PlatformContactId = DomainGuard.Optional(platformContactId, nameof(platformContactId), 128);
        CreatedUtc = DomainGuard.Utc(createdUtc, nameof(createdUtc));
        UpdatedUtc = CreatedUtc;
    }

    private WhatsAppContact()
    {
        PhoneNumberHash = string.Empty;
        EncryptedPhoneNumber = string.Empty;
    }

    public string PhoneNumberHash { get; private set; }
    public string EncryptedPhoneNumber { get; private set; }
    public string? PlatformContactId { get; private set; }
    public bool IsVerified { get; private set; }
    public DateTime? VerifiedUtc { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public DateTime UpdatedUtc { get; private set; }

    public static WhatsAppContact Create(
        string id,
        string userId,
        string phoneNumberHash,
        string encryptedPhoneNumber,
        string? platformContactId,
        DateTime createdUtc) =>
        new(id, userId, phoneNumberHash, encryptedPhoneNumber, platformContactId, createdUtc);

    public void MarkVerified(string? platformContactId, DateTime verifiedUtc)
    {
        PlatformContactId = DomainGuard.Optional(platformContactId, nameof(platformContactId), 128) ?? PlatformContactId;
        VerifiedUtc = DomainGuard.Utc(verifiedUtc, nameof(verifiedUtc));
        UpdatedUtc = VerifiedUtc.Value;
        IsVerified = true;
    }
}
