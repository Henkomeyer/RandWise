using System.Security.Claims;
using RandWise.Application.Budgeting;
using RandWise.Contracts.RecurringTransactions;
using AppException = RandWise.Application.Common.ApplicationException;

namespace RandWise.Api.Endpoints;

public static class RecurringTransactionEndpoints
{
    public static RouteGroupBuilder MapRecurringTransactionEndpoints(this RouteGroupBuilder api)
    {
        var recurring = api.MapGroup("/recurring-transactions")
            .RequireAuthorization()
            .WithTags("Recurring transactions");

        recurring.MapGet("/", async (
                ClaimsPrincipal user,
                IRecurringTransactionService service,
                CancellationToken cancellationToken) =>
            await RunAsync(() => service.ListAsync(user.GetRequiredUserId(), cancellationToken)))
            .WithName("ListRecurringTransactions");

        recurring.MapPost("/", async (
                RecurringTransactionRequest request,
                ClaimsPrincipal user,
                IRecurringTransactionService service,
                CancellationToken cancellationToken) =>
            await RunAsync(async () =>
            {
                var response = await service.CreateAsync(user.GetRequiredUserId(), request, cancellationToken);
                return Results.Created($"/api/v1/recurring-transactions/{response.Id}", response);
            }))
            .WithName("CreateRecurringTransaction");

        recurring.MapPut("/{id}", async (
                string id,
                RecurringTransactionRequest request,
                ClaimsPrincipal user,
                IRecurringTransactionService service,
                CancellationToken cancellationToken) =>
            await RunAsync(() => service.UpdateAsync(user.GetRequiredUserId(), id, request, cancellationToken)))
            .WithName("UpdateRecurringTransaction");

        recurring.MapDelete("/{id}", async (
                string id,
                ClaimsPrincipal user,
                IRecurringTransactionService service,
                CancellationToken cancellationToken) =>
            await RunAsync(async () =>
            {
                await service.DeleteAsync(user.GetRequiredUserId(), id, cancellationToken);
                return Results.NoContent();
            }))
            .WithName("DeleteRecurringTransaction");

        recurring.MapPost("/{id}/pause", async (
                string id,
                ClaimsPrincipal user,
                IRecurringTransactionService service,
                CancellationToken cancellationToken) =>
            await RunAsync(() => service.PauseAsync(user.GetRequiredUserId(), id, cancellationToken)))
            .WithName("PauseRecurringTransaction");

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
