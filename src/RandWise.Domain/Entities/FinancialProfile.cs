using RandWise.Domain.Common;
using RandWise.Domain.Enums;

namespace RandWise.Domain.Entities;

public sealed class FinancialProfile : UserOwnedAggregateRoot
{
    private FinancialProfile(string id, string userId, DateTime createdUtc)
        : base(id, userId)
    {
        BudgetCycleType = BudgetCycleType.CalendarMonth;
        NotificationMode = NotificationMode.Confirm;
        FirstDayOfWeek = DayOfWeek.Monday;
        CreatedUtc = DomainGuard.Utc(createdUtc, nameof(createdUtc));
        UpdatedUtc = CreatedUtc;
    }

    private FinancialProfile()
    {
    }

    public long DefaultMonthlyIncomeCents { get; private set; }
    public int? PaydayDay { get; private set; }
    public BudgetCycleType BudgetCycleType { get; private set; }
    public long StartingBalanceCents { get; private set; }
    public long SafetyBufferCents { get; private set; }
    public long SavingsCommitmentCents { get; private set; }
    public NotificationMode NotificationMode { get; private set; }
    public DayOfWeek FirstDayOfWeek { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public DateTime UpdatedUtc { get; private set; }

    public static FinancialProfile Create(string id, string userId, DateTime createdUtc) =>
        new(id, userId, createdUtc);

    public void Configure(
        long defaultMonthlyIncomeCents,
        int? paydayDay,
        BudgetCycleType budgetCycleType,
        long startingBalanceCents,
        long safetyBufferCents,
        long savingsCommitmentCents,
        NotificationMode notificationMode,
        DayOfWeek firstDayOfWeek,
        DateTime updatedUtc)
    {
        DefaultMonthlyIncomeCents = DomainGuard.NonNegativeCents(defaultMonthlyIncomeCents, nameof(defaultMonthlyIncomeCents));
        PaydayDay = paydayDay is null ? null : DomainGuard.Range(paydayDay.Value, nameof(paydayDay), 1, 31);
        BudgetCycleType = budgetCycleType;
        StartingBalanceCents = startingBalanceCents;
        SafetyBufferCents = DomainGuard.NonNegativeCents(safetyBufferCents, nameof(safetyBufferCents));
        SavingsCommitmentCents = DomainGuard.NonNegativeCents(savingsCommitmentCents, nameof(savingsCommitmentCents));
        NotificationMode = notificationMode;
        FirstDayOfWeek = firstDayOfWeek;
        UpdatedUtc = DomainGuard.Utc(updatedUtc, nameof(updatedUtc));
    }
}
