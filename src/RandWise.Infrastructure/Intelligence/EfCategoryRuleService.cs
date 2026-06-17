using Microsoft.EntityFrameworkCore;
using RandWise.Application.Common;
using RandWise.Application.Intelligence;
using RandWise.Contracts.CategoryRules;
using RandWise.Domain.Entities;
using RandWise.Domain.Enums;
using RandWise.Infrastructure.Persistence;
using AppException = RandWise.Application.Common.ApplicationException;

namespace RandWise.Infrastructure.Intelligence;

public sealed class EfCategoryRuleService : ICategoryRuleService
{
    private readonly RandWiseDbContext dbContext;
    private readonly IClock clock;
    private readonly IIdGenerator idGenerator;

    public EfCategoryRuleService(RandWiseDbContext dbContext, IClock clock, IIdGenerator idGenerator)
    {
        this.dbContext = dbContext;
        this.clock = clock;
        this.idGenerator = idGenerator;
    }

    public async Task<IReadOnlyList<CategoryRuleResponse>> ListAsync(string userId, CancellationToken cancellationToken) =>
        await dbContext.UserCategoryRules
            .AsNoTracking()
            .Where(rule => rule.UserId == userId)
            .OrderByDescending(rule => rule.IsActive)
            .ThenByDescending(rule => rule.Priority)
            .ThenBy(rule => rule.MatchValue)
            .Select(rule => ToResponse(rule))
            .ToListAsync(cancellationToken);

    public async Task<CategoryRuleResponse> CreateAsync(
        string userId,
        CategoryRuleRequest request,
        CancellationToken cancellationToken)
    {
        var matchType = ParseMatchType(request.MatchType);
        var categoryExists = await dbContext.BudgetCategories.AnyAsync(
            category => category.Id == request.CategoryId
                && category.IsActive
                && (category.IsSystem || category.UserId == userId),
            cancellationToken);

        if (!categoryExists)
        {
            throw new AppException(ApplicationError.Validation, "Category is invalid.");
        }

        var rule = UserCategoryRule.Create(
            idGenerator.NewId(),
            userId,
            matchType,
            request.MatchValue,
            request.CategoryId,
            request.Priority,
            clock.UtcNow);

        dbContext.UserCategoryRules.Add(rule);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(rule);
    }

    public async Task DeactivateAsync(string userId, string id, CancellationToken cancellationToken)
    {
        var rule = await dbContext.UserCategoryRules.SingleOrDefaultAsync(
            rule => rule.Id == id && rule.UserId == userId,
            cancellationToken);

        if (rule is null)
        {
            throw new AppException(ApplicationError.NotFound, "Category rule was not found.");
        }

        rule.Deactivate(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    internal static CategoryRuleMatchType ParseMatchType(string value) =>
        value.Trim().ToLowerInvariant() switch
        {
            "merchant" => CategoryRuleMatchType.Merchant,
            "keyword" => CategoryRuleMatchType.Keyword,
            _ => throw new AppException(ApplicationError.Validation, "Rule match type is invalid.")
        };

    internal static string ToContract(CategoryRuleMatchType value) =>
        value switch
        {
            CategoryRuleMatchType.Merchant => "merchant",
            CategoryRuleMatchType.Keyword => "keyword",
            _ => throw new AppException(ApplicationError.Validation, "Rule match type is invalid.")
        };

    private static CategoryRuleResponse ToResponse(UserCategoryRule rule) =>
        new(
            rule.Id,
            ToContract(rule.MatchType),
            rule.MatchValue,
            rule.CategoryId,
            rule.Priority,
            rule.IsActive,
            rule.CreatedUtc,
            rule.UpdatedUtc);
}
