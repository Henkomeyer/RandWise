namespace RandWise.Contracts.CategoryRules;

public sealed record CategoryRuleRequest(
    string MatchType,
    string MatchValue,
    string CategoryId,
    int Priority);
