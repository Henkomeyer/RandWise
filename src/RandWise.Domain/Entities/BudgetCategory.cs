using RandWise.Domain.Common;
using RandWise.Domain.Enums;

namespace RandWise.Domain.Entities;

public sealed class BudgetCategory : Entity
{
    private BudgetCategory(
        string id,
        string? userId,
        string name,
        string slug,
        BudgetCategoryType categoryType,
        string? icon,
        int sortOrder,
        bool isSystem,
        DateTime createdUtc)
        : base(id)
    {
        UserId = DomainGuard.Optional(userId, nameof(userId), 128);
        if (!isSystem && UserId is null)
        {
            throw new DomainException("User category must have a user id.");
        }

        Name = DomainGuard.Required(name, nameof(name), 100);
        Slug = DomainGuard.Required(slug, nameof(slug), 120).ToLowerInvariant();
        CategoryType = categoryType;
        Icon = DomainGuard.Optional(icon, nameof(icon), 64);
        SortOrder = sortOrder;
        IsSystem = isSystem;
        IsActive = true;
        CreatedUtc = DomainGuard.Utc(createdUtc, nameof(createdUtc));
        UpdatedUtc = CreatedUtc;
    }

    private BudgetCategory()
    {
        Name = string.Empty;
        Slug = string.Empty;
    }

    public string? UserId { get; private set; }
    public string Name { get; private set; }
    public string Slug { get; private set; }
    public BudgetCategoryType CategoryType { get; private set; }
    public string? Icon { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsSystem { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public DateTime UpdatedUtc { get; private set; }

    public static BudgetCategory CreateSystem(
        string id,
        string name,
        string slug,
        BudgetCategoryType categoryType,
        string? icon,
        int sortOrder,
        DateTime createdUtc) =>
        new(id, null, name, slug, categoryType, icon, sortOrder, true, createdUtc);

    public static BudgetCategory CreateUser(
        string id,
        string userId,
        string name,
        string slug,
        BudgetCategoryType categoryType,
        string? icon,
        int sortOrder,
        DateTime createdUtc) =>
        new(id, userId, name, slug, categoryType, icon, sortOrder, false, createdUtc);

    public void Rename(string name, string slug, string? icon, int sortOrder, DateTime updatedUtc)
    {
        Name = DomainGuard.Required(name, nameof(name), 100);
        Slug = DomainGuard.Required(slug, nameof(slug), 120).ToLowerInvariant();
        Icon = DomainGuard.Optional(icon, nameof(icon), 64);
        SortOrder = sortOrder;
        UpdatedUtc = DomainGuard.Utc(updatedUtc, nameof(updatedUtc));
    }

    public void Deactivate(DateTime updatedUtc)
    {
        IsActive = false;
        UpdatedUtc = DomainGuard.Utc(updatedUtc, nameof(updatedUtc));
    }
}
