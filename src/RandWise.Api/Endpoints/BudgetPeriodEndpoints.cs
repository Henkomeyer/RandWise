using System.Security.Claims;
using RandWise.Application.Budgeting;
using RandWise.Application.Common;
using RandWise.Contracts.Budgeting;
using AppException = RandWise.Application.Common.ApplicationException;

namespace RandWise.Api.Endpoints;

public static class BudgetPeriodEndpoints
{
    public static RouteGroupBuilder MapBudgetPeriodEndpoints(this RouteGroupBuilder api)
    {
        var periods = api.MapGroup("/budget-periods")
            .RequireAuthorization()
            .WithTags("Budget periods");

        periods.MapGet("/", async (
                ClaimsPrincipal user,
                IBudgetPeriodService service,
                CancellationToken cancellationToken) =>
            await RunAsync(() => service.ListAsync(user.GetRequiredUserId(), cancellationToken)))
            .WithName("ListBudgetPeriods");

        periods.MapGet("/current", async (
                ClaimsPrincipal user,
                IBudgetPeriodService service,
                IClock clock,
                CancellationToken cancellationToken) =>
            await RunAsync(() => service.GetCurrentAsync(
                user.GetRequiredUserId(),
                DateOnly.FromDateTime(clock.UtcNow),
                cancellationToken)))
            .WithName("GetCurrentBudgetPeriod");

        periods.MapGet("/{id}", async (
                string id,
                ClaimsPrincipal user,
                IBudgetPeriodService service,
                CancellationToken cancellationToken) =>
            {
                var response = await service.GetAsync(user.GetRequiredUserId(), id, cancellationToken);
                return response is null ? Results.NotFound() : Results.Ok(response);
            })
            .WithName("GetBudgetPeriod");

        periods.MapPost("/", async (
                BudgetPeriodRequest request,
                ClaimsPrincipal user,
                IBudgetPeriodService service,
                CancellationToken cancellationToken) =>
            await RunAsync(async () =>
            {
                var response = await service.CreateAsync(user.GetRequiredUserId(), request, cancellationToken);
                return Results.Created($"/api/v1/budget-periods/{response.Id}", response);
            }))
            .WithName("CreateBudgetPeriod");

        periods.MapPut("/{id}", async (
                string id,
                BudgetPeriodRequest request,
                ClaimsPrincipal user,
                IBudgetPeriodService service,
                CancellationToken cancellationToken) =>
            await RunAsync(() => service.UpdateAsync(user.GetRequiredUserId(), id, request, cancellationToken)))
            .WithName("UpdateBudgetPeriod");

        periods.MapPost("/{id}/close", async (
                string id,
                ClaimsPrincipal user,
                IBudgetPeriodService service,
                CancellationToken cancellationToken) =>
            await RunAsync(() => service.CloseAsync(user.GetRequiredUserId(), id, cancellationToken)))
            .WithName("CloseBudgetPeriod");

        periods.MapCategoryBudgetEndpoints();

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

    private static async Task<IResult> RunAsync(Func<Task<IResult>> operation)
    {
        try
        {
            return await operation();
        }
        catch (AppException exception)
        {
            return exception.ToProblem();
        }
    }
}
