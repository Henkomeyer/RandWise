using Microsoft.EntityFrameworkCore;
using RandWise.Application.Common;
using RandWise.Application.Jobs;
using RandWise.Application.Transactions;
using RandWise.Contracts.Transactions;
using RandWise.Domain.Entities;
using RandWise.Domain.Enums;
using RandWise.Infrastructure.Persistence;

namespace RandWise.Infrastructure.Jobs;

public sealed class EfRecurringTransactionGenerator : IRecurringTransactionGenerator
{
    private readonly IClock clock;
    private readonly RandWiseDbContext dbContext;
    private readonly ITransactionService transactionService;

    public EfRecurringTransactionGenerator(
        IClock clock,
        RandWiseDbContext dbContext,
        ITransactionService transactionService)
    {
        this.clock = clock;
        this.dbContext = dbContext;
        this.transactionService = transactionService;
    }

    public async Task<int> GenerateDueAsync(DateOnly today, CancellationToken cancellationToken)
    {
        var dueItems = await dbContext.RecurringTransactions
            .Where(item => item.IsActive
                && item.AutoCreate
                && item.NextOccurrenceDate <= today
                && (item.EndDate == null || item.EndDate >= item.NextOccurrenceDate))
            .OrderBy(item => item.NextOccurrenceDate)
            .Take(100)
            .ToListAsync(cancellationToken);

        foreach (var item in dueItems)
        {
            await transactionService.CreateFromRecurringAsync(
                item.UserId,
                item.Id,
                new CreateTransactionRequest(
                    item.AmountCents,
                    item.TransactionType.ToContract(),
                    item.CategoryId,
                    item.Description,
                    item.Merchant,
                    item.NextOccurrenceDate,
                    "recurring"),
                cancellationToken);

            item.MoveNextOccurrence(NextOccurrence(item), clock.UtcNow);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return dueItems.Count;
    }

    private static DateOnly NextOccurrence(RecurringTransaction item) =>
        item.Frequency switch
        {
            RecurrenceFrequency.Weekly => item.NextOccurrenceDate.AddDays(7),
            RecurrenceFrequency.Monthly => AddMonthClamped(item.NextOccurrenceDate, item.DayOfMonth),
            _ => item.NextOccurrenceDate.AddDays(1)
        };

    private static DateOnly AddMonthClamped(DateOnly current, int? dayOfMonth)
    {
        var next = current.AddMonths(1);
        var targetDay = Math.Min(dayOfMonth ?? current.Day, DateTime.DaysInMonth(next.Year, next.Month));
        return new DateOnly(next.Year, next.Month, targetDay);
    }
}
