using Microsoft.EntityFrameworkCore;
using RandWise.Application.Intelligence;
using RandWise.Domain.Enums;
using RandWise.Infrastructure.Persistence;

namespace RandWise.Infrastructure.Intelligence;

public sealed class EfCategoryClassificationService : ICategoryClassificationService
{
    private static readonly IReadOnlyDictionary<string, string[]> SystemKeywordRules = new Dictionary<string, string[]>
    {
        ["petrol"] = ["petrol", "fuel", "shell", "engen", "bp", "caltex", "sasol"],
        ["groceries"] = ["grocery", "groceries", "shoprite", "checkers", "pick n pay", "woolworths"],
        ["takeaways"] = ["takeaway", "takeaways", "kfc", "mcdonald", "nandos", "steers", "uber eats", "mr d"],
        ["transport"] = ["uber", "bolt", "taxi", "bus", "train", "gautrain"],
        ["income"] = ["salary", "income", "paid", "tutoring", "side hustle"]
    };

    private readonly RandWiseDbContext dbContext;
    private readonly IAiCategoryClassifier aiClassifier;

    public EfCategoryClassificationService(RandWiseDbContext dbContext, IAiCategoryClassifier aiClassifier)
    {
        this.dbContext = dbContext;
        this.aiClassifier = aiClassifier;
    }

    public async Task<CategoryClassificationResult> ClassifyAsync(
        string userId,
        string description,
        string? merchant,
        CancellationToken cancellationToken)
    {
        var normalizedDescription = Normalize(description);
        var normalizedMerchant = NormalizeOptional(merchant);

        var personalRule = await dbContext.UserCategoryRules
            .AsNoTracking()
            .Where(rule => rule.UserId == userId && rule.IsActive)
            .OrderByDescending(rule => rule.Priority)
            .FirstOrDefaultAsync(rule =>
                    (rule.MatchType == CategoryRuleMatchType.Merchant
                        && normalizedMerchant != null
                        && normalizedMerchant.Contains(rule.NormalizedMatchValue))
                    || (rule.MatchType == CategoryRuleMatchType.Keyword
                        && normalizedDescription.Contains(rule.NormalizedMatchValue)),
                cancellationToken);

        if (personalRule is not null)
        {
            return new CategoryClassificationResult(personalRule.CategoryId, 9800, "personal-rule");
        }

        var systemMatch = await MatchSystemRuleAsync(normalizedDescription, normalizedMerchant, cancellationToken);
        if (systemMatch is not null)
        {
            return new CategoryClassificationResult(systemMatch, 9200, "system-rule");
        }

        var candidates = await dbContext.BudgetCategories
            .AsNoTracking()
            .Where(category => category.IsActive && (category.IsSystem || category.UserId == userId))
            .OrderBy(category => category.SortOrder)
            .Select(category => category.Name)
            .ToListAsync(cancellationToken);

        var aiResult = await aiClassifier.ClassifyAsync(
            new AiClassificationRequest(description, merchant, candidates),
            cancellationToken);

        if (aiResult?.CategoryName is not null)
        {
            var category = await dbContext.BudgetCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    category => category.IsActive
                        && (category.IsSystem || category.UserId == userId)
                        && category.Name == aiResult.CategoryName,
                    cancellationToken);

            if (category is not null)
            {
                return new CategoryClassificationResult(category.Id, aiResult.ConfidenceBasisPoints, $"ai:{aiResult.Provider}");
            }
        }

        return new CategoryClassificationResult(null, 7000, "uncategorised");
    }

    private async Task<string?> MatchSystemRuleAsync(string normalizedDescription, string? normalizedMerchant, CancellationToken cancellationToken)
    {
        var haystack = normalizedMerchant is null
            ? normalizedDescription
            : $"{normalizedDescription} {normalizedMerchant}";

        foreach (var (slug, keywords) in SystemKeywordRules)
        {
            if (keywords.Any(haystack.Contains))
            {
                var category = await dbContext.BudgetCategories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(category => category.IsActive && category.Slug == slug, cancellationToken);

                if (category is not null)
                {
                    return category.Id;
                }
            }
        }

        return null;
    }

    private static string Normalize(string value) =>
        value.Trim().ToLowerInvariant();

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : Normalize(value);
}
