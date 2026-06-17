using System.Security.Claims;
using RandWise.Application.Budgeting;
using RandWise.Application.Common;
using AppException = RandWise.Application.Common.ApplicationException;

namespace RandWise.Api.Endpoints;

public static class DashboardEndpoints
{
    public static RouteGroupBuilder MapDashboardEndpoints(this RouteGroupBuilder api)
    {
        var dashboard = api.MapGroup("/dashboard")
            .RequireAuthorization()
            .WithTags("Dashboard");

        dashboard.MapGet("/safe-to-spend", async (
                ClaimsPrincipal user,
                ISafeToSpendService service,
                IClock clock,
                CancellationToken cancellationToken) =>
            await RunAsync(() => service.GetCurrentAsync(
                user.GetRequiredUserId(),
                DateOnly.FromDateTime(clock.UtcNow),
                cancellationToken)))
            .WithName("GetSafeToSpend");

        return api;
    }

    private static async Task<IResult> RunAsync<T>(Func<Task<T>> operation)
    {
        try
        {
            return Results.Ok(await operation());
        }
        catch (AppException exception)
        {
            return exception.ToProblem();
        }
    }
}
