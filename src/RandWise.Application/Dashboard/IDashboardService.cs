using RandWise.Contracts.Dashboard;

namespace RandWise.Application.Dashboard;

public interface IDashboardService
{
    Task<DashboardResponse> GetAsync(string userId, DateOnly today, CancellationToken cancellationToken);
}
