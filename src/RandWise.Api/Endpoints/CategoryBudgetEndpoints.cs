using System.Security.Claims;
using RandWise.Application.Budgeting;
using RandWise.Contracts.Budgeting;
using AppException = RandWise.Application.Common.ApplicationException;

namespace RandWise.Api.Endpoints;

public static class CategoryBudgetEndpoints
{
    public static RouteGroupBuilder MapCategoryBudgetEndpoints(this RouteGroupBuilder periods)
    {
        periods.MapGet("/{periodId}/category-budgets", async (
                string periodId,
                ClaimsPrincipal user,
                ICategoryBudgetService service,
                CancellationToken cancellationToken) =>
            await RunAsync(() => service.ListAsync(user.GetRequiredUserId(), periodId, cancellationToken)))
            .WithName("ListCategoryBudgets");

        periods.MapPost("/{periodId}/category-budgets", async (
                string periodId,
                CategoryBudgetRequest request,
                ClaimsPrincipal user,
                ICategoryBudgetService service,
                CancellationToken cancellationToken) =>
            await RunAsync(async () =>
            {
                var response = await service.CreateAsync(user.GetRequiredUserId(), periodId, request, cancellationToken);
                return Results.Created($"/api/v1/category-budgets/{response.Id}", response);
            }))
            .WithName("CreateCategoryBudget");

        return periods;
    }

    public static RouteGroupBuilder MapCategoryBudgetRootEndpoints(this RouteGroupBuilder api)
    {
        var budgets = api.MapGroup("/category-budgets")
            .RequireAuthorization()
            .WithTags("Category budgets");

        budgets.MapPut("/{id}", async (
                string id,
                CategoryBudgetRequest request,
                ClaimsPrincipal user,
                ICategoryBudgetService service,
                CancellationToken cancellationToken) =>
            await RunAsync(() => service.UpdateAsync(user.GetRequiredUserId(), id, request, cancellationToken)))
            .WithName("UpdateCategoryBudget");

        budgets.MapDelete("/{id}", async (
                string id,
                ClaimsPrincipal user,
                ICategoryBudgetService service,
                CancellationToken cancellationToken) =>
            await RunAsync(async () =>
            {
                await service.DeleteAsync(user.GetRequiredUserId(), id, cancellationToken);
                return Results.NoContent();
            }))
            .WithName("DeleteCategoryBudget");

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
