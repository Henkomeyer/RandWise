using RandWise.Domain.Common;
using RandWise.Domain.Enums;

namespace RandWise.Domain.Entities;

public sealed class BudgetPeriod : UserOwnedAggregateRoot
{
    private BudgetPeriod(
        string id,
        string userId,
        DateOnly startDate,
        DateOnly endDate,
        long expectedIncomeCents,
        long openingBalanceCents,
        DateTime createdUtc)
        : base(id, userId)
    {
        DomainGuard.DateRange(startDate, endDate);
        StartDate = startDate;
        EndDate = endDate;
        ExpectedIncomeCents = DomainGuard.NonNegativeCents(expectedIncomeCents, nameof(expectedIncomeCents));
        OpeningBalanceCents = openingBalanceCents;
        Status = BudgetPeriodStatus.Open;
        CreatedUtc = DomainGuard.Utc(createdUtc, nameof(createdUtc));
        UpdatedUtc = CreatedUtc;
    }

    private BudgetPeriod()
    {
    }

    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public long ExpectedIncomeCents { get; private set; }
    public long ActualIncomeCents { get; private set; }
    public long OpeningBalanceCents { get; private set; }
    public BudgetPeriodStatus Status { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public DateTime UpdatedUtc { get; private set; }

    public static BudgetPeriod Create(
        string id,
        string userId,
        DateOnly startDate,
        DateOnly endDate,
        long expectedIncomeCents,
        long openingBalanceCents,
        DateTime createdUtc) =>
        new(id, userId, startDate, endDate, expectedIncomeCents, openingBalanceCents, createdUtc);

    public int DaysRemaining(DateOnly today)
    {
        if (today > EndDate)
        {
            return 0;
        }

        return EndDate.DayNumber - today.DayNumber + 1;
    }

    public void UpdateIncome(long expectedIncomeCents, long actualIncomeCents, DateTime updatedUtc)
    {
        ExpectedIncomeCents = DomainGuard.NonNegativeCents(expectedIncomeCents, nameof(expectedIncomeCents));
        ActualIncomeCents = DomainGuard.NonNegativeCents(actualIncomeCents, nameof(actualIncomeCents));
        UpdatedUtc = DomainGuard.Utc(updatedUtc, nameof(updatedUtc));
    }

    public void UpdateDetails(
        DateOnly startDate,
        DateOnly endDate,
        long expectedIncomeCents,
        long openingBalanceCents,
        DateTime updatedUtc)
    {
        DomainGuard.DateRange(startDate, endDate);
        StartDate = startDate;
        EndDate = endDate;
        ExpectedIncomeCents = DomainGuard.NonNegativeCents(expectedIncomeCents, nameof(expectedIncomeCents));
        OpeningBalanceCents = openingBalanceCents;
        UpdatedUtc = DomainGuard.Utc(updatedUtc, nameof(updatedUtc));
    }

    public void Close(DateTime closedUtc)
    {
        Status = BudgetPeriodStatus.Closed;
        UpdatedUtc = DomainGuard.Utc(closedUtc, nameof(closedUtc));
    }
}
