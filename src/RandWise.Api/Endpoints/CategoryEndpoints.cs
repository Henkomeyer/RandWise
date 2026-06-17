using System.Security.Claims;
using RandWise.Application.Budgeting;
using RandWise.Contracts.Categories;
using AppException = RandWise.Application.Common.ApplicationException;

namespace RandWise.Api.Endpoints;

public static class CategoryEndpoints
{
    public static RouteGroupBuilder MapCategoryEndpoints(this RouteGroupBuilder api)
    {
        var categories = api.MapGroup("/categories")
            .RequireAuthorization()
            .WithTags("Categories");

        categories.MapGet("/", async (
                ClaimsPrincipal user,
                ICategoryService service,
                CancellationToken cancellationToken) =>
            await RunAsync(() => service.ListAsync(user.GetRequiredUserId(), cancellationToken)))
            .WithName("ListCategories");

        categories.MapPost("/", async (
                CategoryRequest request,
                ClaimsPrincipal user,
                ICategoryService service,
                CancellationToken cancellationToken) =>
            await RunAsync(async () =>
            {
                var response = await service.CreateAsync(user.GetRequiredUserId(), request, cancellationToken);
                return Results.Created($"/api/v1/categories/{response.Id}", response);
            }))
            .WithName("CreateCategory");

        categories.MapPut("/{id}", async (
                string id,
                CategoryRequest request,
                ClaimsPrincipal user,
                ICategoryService service,
                CancellationToken cancellationToken) =>
            await RunAsync(() => service.UpdateAsync(user.GetRequiredUserId(), id, request, cancellationToken)))
            .WithName("UpdateCategory");

        categories.MapDelete("/{id}", async (
                string id,
                ClaimsPrincipal user,
                ICategoryService service,
                CancellationToken cancellationToken) =>
            await RunAsync(async () =>
            {
                await service.DeleteAsync(user.GetRequiredUserId(), id, cancellationToken);
                return Results.NoContent();
            }))
            .WithName("DeleteCategory");

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
