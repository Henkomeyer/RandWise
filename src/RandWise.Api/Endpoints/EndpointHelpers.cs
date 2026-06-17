using System.Security.Claims;
using RandWise.Application.Common;
using AppException = RandWise.Application.Common.ApplicationException;

namespace RandWise.Api.Endpoints;

internal static class EndpointHelpers
{
    public static string GetRequiredUserId(this ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue("app_user_id")
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new AppException(ApplicationError.Unauthorized, "Authenticated principal has no user id.");
        }

        return userId;
    }

    public static IResult ToProblem(this AppException exception) =>
        exception.Error switch
        {
            ApplicationError.NotFound => Results.Problem(
                title: "Not found.",
                detail: exception.Message,
                statusCode: StatusCodes.Status404NotFound),
            ApplicationError.Unauthorized => Results.Problem(
                title: "Unauthorized.",
                detail: exception.Message,
                statusCode: StatusCodes.Status401Unauthorized),
            _ => Results.Problem(
                title: "Invalid request.",
                detail: exception.Message,
                statusCode: StatusCodes.Status400BadRequest)
        };
}
