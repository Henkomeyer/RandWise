using RandWise.Contracts.Budgeting;

namespace RandWise.Application.Budgeting;

public interface ICategoryBudgetService
{
    Task<IReadOnlyList<CategoryBudgetResponse>> ListAsync(string userId, string budgetPeriodId, CancellationToken cancellationToken);

    Task<CategoryBudgetResponse> CreateAsync(string userId, string budgetPeriodId, CategoryBudgetRequest request, CancellationToken cancellationToken);

    Task<CategoryBudgetResponse> UpdateAsync(string userId, string id, CategoryBudgetRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(string userId, string id, CancellationToken cancellationToken);
}
