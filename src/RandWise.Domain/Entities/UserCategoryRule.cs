using RandWise.Domain.Common;
using RandWise.Domain.Enums;

namespace RandWise.Domain.Entities;

public sealed class UserCategoryRule : UserOwnedAggregateRoot
{
    private UserCategoryRule(
        string id,
        string userId,
        CategoryRuleMatchType matchType,
        string matchValue,
        string categoryId,
        int priority,
        DateTime createdUtc)
        : base(id, userId)
    {
        MatchType = matchType;
        MatchValue = DomainGuard.Required(matchValue, nameof(matchValue), 160);
        NormalizedMatchValue = MatchValue.Trim().ToLowerInvariant();
        CategoryId = DomainGuard.Required(categoryId, nameof(categoryId), 128);
        Priority = priority;
        IsActive = true;
        CreatedUtc = DomainGuard.Utc(createdUtc, nameof(createdUtc));
        UpdatedUtc = CreatedUtc;
    }

    private UserCategoryRule()
    {
        MatchValue = string.Empty;
        NormalizedMatchValue = string.Empty;
        CategoryId = string.Empty;
    }

    public CategoryRuleMatchType MatchType { get; private set; }
    public string MatchValue { get; private set; }
    public string NormalizedMatchValue { get; private set; }
    public string CategoryId { get; private set; }
    public int Priority { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public DateTime UpdatedUtc { get; private set; }

    public static UserCategoryRule Create(
        string id,
        string userId,
        CategoryRuleMatchType matchType,
        string matchValue,
        string categoryId,
        int priority,
        DateTime createdUtc) =>
        new(id, userId, matchType, matchValue, categoryId, priority, createdUtc);

    public void Deactivate(DateTime updatedUtc)
    {
        IsActive = false;
        UpdatedUtc = DomainGuard.Utc(updatedUtc, nameof(updatedUtc));
    }
}
