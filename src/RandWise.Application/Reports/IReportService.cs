using RandWise.Contracts.Reports;

namespace RandWise.Application.Reports;

public interface IReportService
{
    Task<WeeklyFinancialStoryResponse> GetWeeklyAsync(
        string userId,
        DateOnly weekStart,
        CancellationToken cancellationToken);

    Task<MonthlyMoneyWrapResponse> GetMonthlyAsync(
        string userId,
        int year,
        int month,
        CancellationToken cancellationToken);

    Task<CategoryBreakdownResponse> GetCategoryBreakdownAsync(
        string userId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken);

    Task<string> ExportTransactionsCsvAsync(
        string userId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken);
}
