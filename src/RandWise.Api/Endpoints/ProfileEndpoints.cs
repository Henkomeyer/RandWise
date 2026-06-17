using System.Security.Claims;
using RandWise.Application.Privacy;
using AppException = RandWise.Application.Common.ApplicationException;

namespace RandWise.Api.Endpoints;

public static class ProfileEndpoints
{
    public static RouteGroupBuilder MapProfileEndpoints(this RouteGroupBuilder api)
    {
        var profile = api.MapGroup("/profile")
            .RequireAuthorization()
            .WithTags("Profile");

        profile.MapGet("/export", async (
                ClaimsPrincipal user,
                IPrivacyService service,
                CancellationToken cancellationToken) =>
            await RunAsync(() => service.ExportProfileAsync(user.GetRequiredUserId(), cancellationToken)))
            .WithName("ExportProfileData");

        profile.MapDelete("/", async (
                ClaimsPrincipal user,
                IPrivacyService service,
                CancellationToken cancellationToken) =>
            await RunNoContentAsync(async () =>
            {
                await service.DeleteAccountAsync(user.GetRequiredUserId(), cancellationToken);
                return Results.NoContent();
            }))
            .WithName("DeleteProfile");

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

    private static async Task<IResult> RunNoContentAsync(Func<Task<IResult>> operation)
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
