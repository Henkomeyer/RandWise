using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RandWise.Application.Audit;
using RandWise.Application.Common;
using RandWise.Application.Reports;
using RandWise.Contracts.Reports;
using RandWise.Domain.Entities;
using RandWise.Domain.Enums;
using RandWise.Infrastructure.Persistence;

namespace RandWise.Infrastructure.Reports;

public sealed class EfReportService : IReportService
{
    private readonly RandWiseDbContext dbContext;
    private readonly IAuditLogService auditLogService;

    public EfReportService(RandWiseDbContext dbContext, IAuditLogService auditLogService)
    {
        this.dbContext = dbContext;
        this.auditLogService = auditLogService;
    }

    public async Task<WeeklyFinancialStoryResponse> GetWeeklyAsync(
        string userId,
        DateOnly weekStart,
        CancellationToken cancellationToken)
    {
        var weekEnd = weekStart.AddDays(6);
        var transactions = await LoadTransactionsAsync(userId, weekStart, weekEnd, cancellationToken);
        var income = transactions
            .Where(row => row.Transaction.TransactionType == TransactionType.Income)
            .Sum(row => row.Transaction.AmountCents);
        var expense = transactions
            .Where(row => row.Transaction.TransactionType == TransactionType.Expense)
            .Sum(row => row.Transaction.AmountCents);

        await auditLogService.RecordAsync(userId, "report.weekly.viewed", "Report", "weekly", null, cancellationToken);

        return new WeeklyFinancialStoryResponse(
            weekStart,
            weekEnd,
            income,
            expense,
            income - expense,
            transactions.Count,
            BuildSummary(income, expense, "week"),
            BuildTopCategories(transactions));
    }

    public async Task<MonthlyMoneyWrapResponse> GetMonthlyAsync(
        string userId,
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        var from = new DateOnly(year, month, 1);
        var to = from.AddMonths(1).AddDays(-1);
        var transactions = await LoadTransactionsAsync(userId, from, to, cancellationToken);
        var income = transactions
            .Where(row => row.Transaction.TransactionType == TransactionType.Income)
            .Sum(row => row.Transaction.AmountCents);
        var expense = transactions
            .Where(row => row.Transaction.TransactionType == TransactionType.Expense)
            .Sum(row => row.Transaction.AmountCents);

        await auditLogService.RecordAsync(userId, "report.monthly.viewed", "Report", $"{year:D4}-{month:D2}", null, cancellationToken);

        return new MonthlyMoneyWrapResponse(
            year,
            month,
            income,
            expense,
            income - expense,
            expense / DateTime.DaysInMonth(year, month),
            BuildSummary(income, expense, "month"),
            BuildTopCategories(transactions));
    }

    public async Task<CategoryBreakdownResponse> GetCategoryBreakdownAsync(
        string userId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        var transactions = await LoadTransactionsAsync(userId, from, to, cancellationToken);
        return new CategoryBreakdownResponse(from, to, BuildTopCategories(transactions, int.MaxValue));
    }

    public async Task<string> ExportTransactionsCsvAsync(
        string userId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken)
    {
        var start = from ?? DateOnly.MinValue;
        var end = to ?? DateOnly.MaxValue;
        var transactions = await LoadTransactionsAsync(userId, start, end, cancellationToken);
        var builder = new StringBuilder();
        builder.AppendLine("date,type,amountInCents,category,description,merchant,source,status");

        foreach (var row in transactions.OrderBy(row => row.Transaction.TransactionDate).ThenBy(row => row.Transaction.CreatedUtc))
        {
            builder.Append(row.Transaction.TransactionDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append(',');
            builder.Append(row.Transaction.TransactionType.ToContract()).Append(',');
            builder.Append(row.Transaction.AmountCents.ToString(CultureInfo.InvariantCulture)).Append(',');
            builder.Append(EscapeCsv(row.Category.Name)).Append(',');
            builder.Append(EscapeCsv(row.Transaction.Description)).Append(',');
            builder.Append(EscapeCsv(row.Transaction.Merchant)).Append(',');
            builder.Append(row.Transaction.Source.ToContract()).Append(',');
            builder.Append(row.Transaction.Status.ToContract()).AppendLine();
        }

        await auditLogService.RecordAsync(userId, "report.csv.exported", "Report", "transactions-csv", null, cancellationToken);
        return builder.ToString();
    }

    private async Task<List<TransactionCategoryRow>> LoadTransactionsAsync(
        string userId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken) =>
        await dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.UserId == userId
                && transaction.DeletedUtc == null
                && transaction.TransactionDate >= from
                && transaction.TransactionDate <= to)
            .Join(
                dbContext.BudgetCategories.AsNoTracking(),
                transaction => transaction.CategoryId,
                category => category.Id,
                (transaction, category) => new TransactionCategoryRow(transaction, category))
            .ToListAsync(cancellationToken);

    private static IReadOnlyList<ReportCategoryTotalResponse> BuildTopCategories(
        IReadOnlyList<TransactionCategoryRow> rows,
        int take = 5) =>
        rows
            .Where(row => row.Transaction.TransactionType == TransactionType.Expense)
            .GroupBy(row => new { row.Category.Id, row.Category.Name })
            .Select(group => new ReportCategoryTotalResponse(
                group.Key.Id,
                group.Key.Name,
                group.Sum(row => row.Transaction.AmountCents),
                group.Count()))
            .OrderByDescending(row => row.AmountInCents)
            .ThenBy(row => row.CategoryName)
            .Take(take)
            .ToList();

    private static string BuildSummary(long income, long expense, string periodName)
    {
        if (income == 0 && expense == 0)
        {
            return $"No money movement was recorded this {periodName}.";
        }

        return income >= expense
            ? $"Income covered recorded spending this {periodName}."
            : $"Recorded spending was higher than income this {periodName}.";
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : value;
    }

    private sealed record TransactionCategoryRow(Transaction Transaction, BudgetCategory Category);
}
