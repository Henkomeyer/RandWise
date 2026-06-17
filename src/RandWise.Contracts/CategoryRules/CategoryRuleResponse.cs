namespace RandWise.Contracts.CategoryRules;

public sealed record CategoryRuleResponse(
    string Id,
    string MatchType,
    string MatchValue,
    string CategoryId,
    int Priority,
    bool IsActive,
    DateTime CreatedUtc,
    DateTime UpdatedUtc);
