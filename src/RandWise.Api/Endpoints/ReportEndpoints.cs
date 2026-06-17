using System.Security.Claims;
using System.Text;
using RandWise.Application.Reports;
using AppException = RandWise.Application.Common.ApplicationException;

namespace RandWise.Api.Endpoints;

public static class ReportEndpoints
{
    public static RouteGroupBuilder MapReportEndpoints(this RouteGroupBuilder api)
    {
        var reports = api.MapGroup("/reports")
            .RequireAuthorization()
            .WithTags("Reports");

        reports.MapGet("/weekly", async (
                ClaimsPrincipal user,
                IReportService service,
                DateOnly? weekStart,
                CancellationToken cancellationToken) =>
            await RunAsync(() => service.GetWeeklyAsync(
                user.GetRequiredUserId(),
                weekStart ?? StartOfWeek(DateOnly.FromDateTime(DateTime.UtcNow)),
                cancellationToken)))
            .WithName("GetWeeklyFinancialStory");

        reports.MapGet("/monthly", async (
                ClaimsPrincipal user,
                IReportService service,
                int? year,
                int? month,
                CancellationToken cancellationToken) =>
            await RunAsync(() =>
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                return service.GetMonthlyAsync(user.GetRequiredUserId(), year ?? today.Year, month ?? today.Month, cancellationToken);
            }))
            .WithName("GetMonthlyMoneyWrap");

        reports.MapGet("/category-breakdown", async (
                ClaimsPrincipal user,
                IReportService service,
                DateOnly? from,
                DateOnly? to,
                CancellationToken cancellationToken) =>
            await RunAsync(() =>
            {
                var end = to ?? DateOnly.FromDateTime(DateTime.UtcNow);
                var start = from ?? end.AddDays(-29);
                return service.GetCategoryBreakdownAsync(user.GetRequiredUserId(), start, end, cancellationToken);
            }))
            .WithName("GetCategoryBreakdownReport");

        reports.MapGet("/export/csv", async (
                ClaimsPrincipal user,
                IReportService service,
                DateOnly? from,
                DateOnly? to,
                CancellationToken cancellationToken) =>
            await RunFileAsync(async () =>
            {
                var csv = await service.ExportTransactionsCsvAsync(user.GetRequiredUserId(), from, to, cancellationToken);
                return Results.File(
                    Encoding.UTF8.GetBytes(csv),
                    "text/csv",
                    $"randwise-transactions-{DateTime.UtcNow:yyyyMMdd}.csv");
            }))
            .WithName("ExportTransactionsCsv");

        return api;
    }

    private static DateOnly StartOfWeek(DateOnly today)
    {
        var offset = ((int)today.DayOfWeek + 6) % 7;
        return today.AddDays(-offset);
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

    private static async Task<IResult> RunFileAsync(Func<Task<IResult>> operation)
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
