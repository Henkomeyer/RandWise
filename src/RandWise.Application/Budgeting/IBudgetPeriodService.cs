using RandWise.Contracts.Budgeting;

namespace RandWise.Application.Budgeting;

public interface IBudgetPeriodService
{
    Task<IReadOnlyList<BudgetPeriodResponse>> ListAsync(string userId, CancellationToken cancellationToken);

    Task<BudgetPeriodResponse?> GetCurrentAsync(string userId, DateOnly today, CancellationToken cancellationToken);

    Task<BudgetPeriodResponse?> GetAsync(string userId, string id, CancellationToken cancellationToken);

    Task<BudgetPeriodResponse> CreateAsync(string userId, BudgetPeriodRequest request, CancellationToken cancellationToken);

    Task<BudgetPeriodResponse> UpdateAsync(string userId, string id, BudgetPeriodRequest request, CancellationToken cancellationToken);

    Task<BudgetPeriodResponse> CloseAsync(string userId, string id, CancellationToken cancellationToken);
}
