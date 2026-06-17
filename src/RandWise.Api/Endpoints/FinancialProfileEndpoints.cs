using System.Security.Claims;
using RandWise.Application.FinancialProfile;
using RandWise.Contracts.FinancialProfile;
using AppException = RandWise.Application.Common.ApplicationException;

namespace RandWise.Api.Endpoints;

public static class FinancialProfileEndpoints
{
    public static RouteGroupBuilder MapFinancialProfileEndpoints(this RouteGroupBuilder api)
    {
        var profile = api.MapGroup("/financial-profile")
            .RequireAuthorization()
            .WithTags("Financial Profile");

        profile.MapGet("/", async (
                ClaimsPrincipal user,
                IFinancialProfileService service,
                CancellationToken cancellationToken) =>
            {
                var response = await service.GetAsync(user.GetRequiredUserId(), cancellationToken);
                return response is null ? Results.NotFound() : Results.Ok(response);
            })
            .WithName("GetFinancialProfile");

        profile.MapPut("/", async (
                FinancialProfileRequest request,
                ClaimsPrincipal user,
                IFinancialProfileService service,
                CancellationToken cancellationToken) =>
            await RunAsync(() => service.UpsertAsync(user.GetRequiredUserId(), request, cancellationToken)))
            .WithName("UpdateFinancialProfile");

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
