using RandWise.Domain.Common;

namespace RandWise.Domain.Entities;

public sealed class CategoryBudget : Entity
{
    private CategoryBudget(
        string id,
        string budgetPeriodId,
        string categoryId,
        long allocatedAmountCents,
        long rolloverAmountCents,
        int warningThresholdPercent,
        DateTime createdUtc)
        : base(id)
    {
        BudgetPeriodId = DomainGuard.Required(budgetPeriodId, nameof(budgetPeriodId), 128);
        CategoryId = DomainGuard.Required(categoryId, nameof(categoryId), 128);
        AllocatedAmountCents = DomainGuard.NonNegativeCents(allocatedAmountCents, nameof(allocatedAmountCents));
        RolloverAmountCents = rolloverAmountCents;
        WarningThresholdPercent = DomainGuard.Range(warningThresholdPercent, nameof(warningThresholdPercent), 1, 100);
        CreatedUtc = DomainGuard.Utc(createdUtc, nameof(createdUtc));
        UpdatedUtc = CreatedUtc;
    }

    private CategoryBudget()
    {
        BudgetPeriodId = string.Empty;
        CategoryId = string.Empty;
    }

    public string BudgetPeriodId { get; private set; }
    public string CategoryId { get; private set; }
    public long AllocatedAmountCents { get; private set; }
    public long RolloverAmountCents { get; private set; }
    public int WarningThresholdPercent { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public DateTime UpdatedUtc { get; private set; }

    public static CategoryBudget Create(
        string id,
        string budgetPeriodId,
        string categoryId,
        long allocatedAmountCents,
        long rolloverAmountCents,
        int warningThresholdPercent,
        DateTime createdUtc) =>
        new(id, budgetPeriodId, categoryId, allocatedAmountCents, rolloverAmountCents, warningThresholdPercent, createdUtc);

    public void Update(long allocatedAmountCents, long rolloverAmountCents, int warningThresholdPercent, DateTime updatedUtc)
    {
        AllocatedAmountCents = DomainGuard.NonNegativeCents(allocatedAmountCents, nameof(allocatedAmountCents));
        RolloverAmountCents = rolloverAmountCents;
        WarningThresholdPercent = DomainGuard.Range(warningThresholdPercent, nameof(warningThresholdPercent), 1, 100);
        UpdatedUtc = DomainGuard.Utc(updatedUtc, nameof(updatedUtc));
    }
}
