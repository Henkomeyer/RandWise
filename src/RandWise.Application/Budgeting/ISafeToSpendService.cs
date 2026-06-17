using RandWise.Contracts.Dashboard;

namespace RandWise.Application.Budgeting;

public interface ISafeToSpendService
{
    Task<SafeToSpendResponse> GetCurrentAsync(string userId, DateOnly today, CancellationToken cancellationToken);
}
