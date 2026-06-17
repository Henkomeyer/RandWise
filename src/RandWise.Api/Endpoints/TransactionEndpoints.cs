using System.Security.Claims;
using RandWise.Application.Transactions;
using RandWise.Contracts.Transactions;
using AppException = RandWise.Application.Common.ApplicationException;

namespace RandWise.Api.Endpoints;

public static class TransactionEndpoints
{
    public static RouteGroupBuilder MapTransactionEndpoints(this RouteGroupBuilder api)
    {
        var transactions = api.MapGroup("/transactions")
            .RequireAuthorization()
            .WithTags("Transactions");

        transactions.MapGet("/", async (
                ClaimsPrincipal user,
                ITransactionService service,
                DateOnly? from,
                DateOnly? to,
                string? categoryId,
                string? type,
                string? source,
                string? search,
                int? page,
                int? pageSize,
                CancellationToken cancellationToken) =>
            await RunAsync(() => service.ListAsync(
                user.GetRequiredUserId(),
                new TransactionQuery(from, to, categoryId, type, source, search, page ?? 1, pageSize ?? 25),
                cancellationToken)))
            .WithName("ListTransactions");

        transactions.MapGet("/{id}", async (
                string id,
                ClaimsPrincipal user,
                ITransactionService service,
                CancellationToken cancellationToken) =>
            {
                var response = await service.GetAsync(user.GetRequiredUserId(), id, cancellationToken);
                return response is null ? Results.NotFound() : Results.Ok(response);
            })
            .WithName("GetTransaction");

        transactions.MapPost("/", async (
                CreateTransactionRequest request,
                ClaimsPrincipal user,
                ITransactionService service,
                CancellationToken cancellationToken) =>
            await RunAsync(async () =>
            {
                var response = await service.CreateAsync(user.GetRequiredUserId(), request, cancellationToken);
                return Results.Created($"/api/v1/transactions/{response.Id}", response);
            }))
            .WithName("CreateTransaction");

        transactions.MapPut("/{id}", async (
                string id,
                UpdateTransactionRequest request,
                ClaimsPrincipal user,
                ITransactionService service,
                CancellationToken cancellationToken) =>
            await RunAsync(() => service.UpdateAsync(user.GetRequiredUserId(), id, request, cancellationToken)))
            .WithName("UpdateTransaction");

        transactions.MapDelete("/{id}", async (
                string id,
                ClaimsPrincipal user,
                ITransactionService service,
                CancellationToken cancellationToken) =>
            await RunAsync(async () =>
            {
                await service.DeleteAsync(user.GetRequiredUserId(), id, cancellationToken);
                return Results.NoContent();
            }))
            .WithName("DeleteTransaction");

        transactions.MapPost("/{id}/restore", async (
                string id,
                ClaimsPrincipal user,
                ITransactionService service,
                CancellationToken cancellationToken) =>
            await RunAsync(() => service.RestoreAsync(user.GetRequiredUserId(), id, cancellationToken)))
            .WithName("RestoreTransaction");

        transactions.MapPost("/{id}/categorise", async (
                string id,
                CategoriseTransactionRequest request,
                ClaimsPrincipal user,
                ITransactionService service,
                CancellationToken cancellationToken) =>
            await RunAsync(() => service.CategoriseAsync(user.GetRequiredUserId(), id, request, cancellationToken)))
            .WithName("CategoriseTransaction");

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
