using RandWise.Domain.Common;
using RandWise.Domain.Enums;

namespace RandWise.Domain.Entities;

public sealed class AppUser : Entity
{
    private AppUser(
        string id,
        string identityUserId,
        string displayName,
        DateTime createdUtc)
        : base(id)
    {
        IdentityUserId = DomainGuard.Required(identityUserId, nameof(identityUserId), 128);
        DisplayName = DomainGuard.Required(displayName, nameof(displayName), 160);
        PreferredCurrency = "ZAR";
        TimeZone = "Africa/Johannesburg";
        PreferredLanguage = "en-ZA";
        Status = AppUserStatus.Active;
        CreatedUtc = DomainGuard.Utc(createdUtc, nameof(createdUtc));
        UpdatedUtc = CreatedUtc;
    }

    private AppUser()
    {
        IdentityUserId = string.Empty;
        DisplayName = string.Empty;
        PreferredCurrency = "ZAR";
        TimeZone = "Africa/Johannesburg";
        PreferredLanguage = "en-ZA";
    }

    public string IdentityUserId { get; private set; }
    public string DisplayName { get; private set; }
    public string PreferredCurrency { get; private set; }
    public string TimeZone { get; private set; }
    public string PreferredLanguage { get; private set; }
    public AppUserStatus Status { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public DateTime UpdatedUtc { get; private set; }
    public DateTime? DeletedUtc { get; private set; }

    public static AppUser Create(string id, string identityUserId, string displayName, DateTime createdUtc) =>
        new(id, identityUserId, displayName, createdUtc);

    public void UpdatePreferences(
        string displayName,
        string preferredCurrency,
        string timeZone,
        string preferredLanguage,
        DateTime updatedUtc)
    {
        DisplayName = DomainGuard.Required(displayName, nameof(displayName), 160);
        PreferredCurrency = DomainGuard.Required(preferredCurrency, nameof(preferredCurrency), 3).ToUpperInvariant();
        TimeZone = DomainGuard.Required(timeZone, nameof(timeZone), 100);
        PreferredLanguage = DomainGuard.Required(preferredLanguage, nameof(preferredLanguage), 16);
        UpdatedUtc = DomainGuard.Utc(updatedUtc, nameof(updatedUtc));
    }

    public void MarkDeleted(DateTime deletedUtc)
    {
        DeletedUtc = DomainGuard.Utc(deletedUtc, nameof(deletedUtc));
        UpdatedUtc = DeletedUtc.Value;
        Status = AppUserStatus.Deleted;
    }
}
