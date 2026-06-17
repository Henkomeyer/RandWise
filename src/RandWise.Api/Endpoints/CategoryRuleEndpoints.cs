using System.Security.Claims;
using RandWise.Application.Intelligence;
using RandWise.Contracts.CategoryRules;
using AppException = RandWise.Application.Common.ApplicationException;

namespace RandWise.Api.Endpoints;

public static class CategoryRuleEndpoints
{
    public static RouteGroupBuilder MapCategoryRuleEndpoints(this RouteGroupBuilder api)
    {
        var rules = api.MapGroup("/category-rules")
            .RequireAuthorization()
            .WithTags("Category rules");

        rules.MapGet("/", async (
                ClaimsPrincipal user,
                ICategoryRuleService service,
                CancellationToken cancellationToken) =>
            await RunAsync(() => service.ListAsync(user.GetRequiredUserId(), cancellationToken)))
            .WithName("ListCategoryRules");

        rules.MapPost("/", async (
                CategoryRuleRequest request,
                ClaimsPrincipal user,
                ICategoryRuleService service,
                CancellationToken cancellationToken) =>
            await RunAsync(() => service.CreateAsync(user.GetRequiredUserId(), request, cancellationToken)))
            .WithName("CreateCategoryRule");

        rules.MapDelete("/{id}", async (
                string id,
                ClaimsPrincipal user,
                ICategoryRuleService service,
                CancellationToken cancellationToken) =>
            await RunAsync(async () =>
            {
                await service.DeactivateAsync(user.GetRequiredUserId(), id, cancellationToken);
                return Results.NoContent();
            }))
            .WithName("DeactivateCategoryRule");

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
