using Microsoft.EntityFrameworkCore;
using RandWise.Application.Budgeting;
using RandWise.Application.Common;
using RandWise.Contracts.Categories;
using RandWise.Domain.Entities;
using RandWise.Infrastructure.Persistence;
using AppException = RandWise.Application.Common.ApplicationException;

namespace RandWise.Infrastructure.Budgeting;

public sealed class EfCategoryService : ICategoryService
{
    private readonly RandWiseDbContext dbContext;
    private readonly IClock clock;
    private readonly IIdGenerator idGenerator;

    public EfCategoryService(RandWiseDbContext dbContext, IClock clock, IIdGenerator idGenerator)
    {
        this.dbContext = dbContext;
        this.clock = clock;
        this.idGenerator = idGenerator;
    }

    public async Task<IReadOnlyList<CategoryResponse>> ListAsync(string userId, CancellationToken cancellationToken) =>
        await dbContext.BudgetCategories
            .AsNoTracking()
            .Where(category => category.IsActive && (category.IsSystem || category.UserId == userId))
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .Select(category => ToResponse(category))
            .ToListAsync(cancellationToken);

    public async Task<CategoryResponse> CreateAsync(string userId, CategoryRequest request, CancellationToken cancellationToken)
    {
        var categoryType = DomainEnumNames.ParseBudgetCategoryType(request.CategoryType);
        var slug = Slugify(request.Name);
        var slugExists = await dbContext.BudgetCategories.AnyAsync(
            category => category.UserId == userId && category.Slug == slug && category.IsActive,
            cancellationToken);

        if (slugExists)
        {
            throw new AppException(ApplicationError.Validation, "Category already exists.");
        }

        var category = BudgetCategory.CreateUser(
            idGenerator.NewId(),
            userId,
            request.Name,
            slug,
            categoryType,
            request.Icon,
            request.SortOrder,
            clock.UtcNow);

        dbContext.BudgetCategories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(category);
    }

    public async Task<CategoryResponse> UpdateAsync(
        string userId,
        string id,
        CategoryRequest request,
        CancellationToken cancellationToken)
    {
        var category = await GetOwnedUserCategoryAsync(userId, id, cancellationToken);
        var categoryType = DomainEnumNames.ParseBudgetCategoryType(request.CategoryType);
        var slug = Slugify(request.Name);
        var slugExists = await dbContext.BudgetCategories.AnyAsync(
            other => other.UserId == userId && other.Id != id && other.Slug == slug && other.IsActive,
            cancellationToken);

        if (slugExists)
        {
            throw new AppException(ApplicationError.Validation, "Category already exists.");
        }

        category.UpdateDetails(request.Name, slug, categoryType, request.Icon, request.SortOrder, clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(category);
    }

    public async Task DeleteAsync(string userId, string id, CancellationToken cancellationToken)
    {
        var category = await GetOwnedUserCategoryAsync(userId, id, cancellationToken);
        var inUse = await dbContext.Transactions.AnyAsync(
                transaction => transaction.UserId == userId && transaction.CategoryId == id,
                cancellationToken)
            || await dbContext.CategoryBudgets.AnyAsync(budget => budget.CategoryId == id, cancellationToken)
            || await dbContext.RecurringTransactions.AnyAsync(
                recurring => recurring.UserId == userId && recurring.CategoryId == id,
                cancellationToken);

        if (inUse)
        {
            category.Deactivate(clock.UtcNow);
        }
        else
        {
            dbContext.BudgetCategories.Remove(category);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    internal static string Slugify(string value)
    {
        var parts = value
            .Trim()
            .ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return string.Join("-", parts);
    }

    private async Task<BudgetCategory> GetOwnedUserCategoryAsync(
        string userId,
        string id,
        CancellationToken cancellationToken)
    {
        var category = await dbContext.BudgetCategories.SingleOrDefaultAsync(
            category => category.Id == id
                && category.UserId == userId
                && !category.IsSystem
                && category.IsActive,
            cancellationToken);

        return category ?? throw new AppException(ApplicationError.NotFound, "Category was not found.");
    }

    private static CategoryResponse ToResponse(BudgetCategory category) =>
        new(
            category.Id,
            category.Name,
            category.Slug,
            category.CategoryType.ToContract(),
            category.Icon,
            category.SortOrder,
            category.IsSystem,
            category.IsActive,
            category.CreatedUtc,
            category.UpdatedUtc);
}
