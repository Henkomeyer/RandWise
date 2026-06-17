using RandWise.Domain.Common;
using RandWise.Domain.Enums;

namespace RandWise.Domain.Entities;

public sealed class RecurringTransaction : UserOwnedAggregateRoot
{
    private RecurringTransaction(
        string id,
        string userId,
        string categoryId,
        string description,
        string? merchant,
        long amountCents,
        TransactionType transactionType,
        RecurrenceFrequency frequency,
        int? dayOfMonth,
        DayOfWeek? dayOfWeek,
        DateOnly nextOccurrenceDate,
        DateOnly? endDate,
        bool autoCreate,
        DateTime createdUtc)
        : base(id, userId)
    {
        CategoryId = DomainGuard.Required(categoryId, nameof(categoryId), 128);
        Description = DomainGuard.Required(description, nameof(description), 280);
        Merchant = DomainGuard.Optional(merchant, nameof(merchant), 160);
        AmountCents = DomainGuard.PositiveCents(amountCents, nameof(amountCents));
        TransactionType = transactionType;
        Frequency = frequency;
        DayOfMonth = dayOfMonth is null ? null : DomainGuard.Range(dayOfMonth.Value, nameof(dayOfMonth), 1, 31);
        DayOfWeek = dayOfWeek;
        NextOccurrenceDate = nextOccurrenceDate;
        EndDate = endDate;
        if (EndDate is not null && EndDate.Value < NextOccurrenceDate)
        {
            throw new DomainException("End date cannot be before next occurrence date.");
        }

        AutoCreate = autoCreate;
        IsActive = true;
        CreatedUtc = DomainGuard.Utc(createdUtc, nameof(createdUtc));
        UpdatedUtc = CreatedUtc;
    }

    private RecurringTransaction()
    {
        CategoryId = string.Empty;
        Description = string.Empty;
    }

    public string CategoryId { get; private set; }
    public string Description { get; private set; }
    public string? Merchant { get; private set; }
    public long AmountCents { get; private set; }
    public TransactionType TransactionType { get; private set; }
    public RecurrenceFrequency Frequency { get; private set; }
    public int? DayOfMonth { get; private set; }
    public DayOfWeek? DayOfWeek { get; private set; }
    public DateOnly NextOccurrenceDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public bool AutoCreate { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public DateTime UpdatedUtc { get; private set; }

    public static RecurringTransaction Create(
        string id,
        string userId,
        string categoryId,
        string description,
        string? merchant,
        long amountCents,
        TransactionType transactionType,
        RecurrenceFrequency frequency,
        int? dayOfMonth,
        DayOfWeek? dayOfWeek,
        DateOnly nextOccurrenceDate,
        DateOnly? endDate,
        bool autoCreate,
        DateTime createdUtc) =>
        new(id, userId, categoryId, description, merchant, amountCents, transactionType, frequency, dayOfMonth, dayOfWeek, nextOccurrenceDate, endDate, autoCreate, createdUtc);

    public void Pause(DateTime updatedUtc)
    {
        IsActive = false;
        UpdatedUtc = DomainGuard.Utc(updatedUtc, nameof(updatedUtc));
    }

    public void UpdateDetails(
        string categoryId,
        string description,
        string? merchant,
        long amountCents,
        TransactionType transactionType,
        RecurrenceFrequency frequency,
        int? dayOfMonth,
        DayOfWeek? dayOfWeek,
        DateOnly nextOccurrenceDate,
        DateOnly? endDate,
        bool autoCreate,
        DateTime updatedUtc)
    {
        CategoryId = DomainGuard.Required(categoryId, nameof(categoryId), 128);
        Description = DomainGuard.Required(description, nameof(description), 280);
        Merchant = DomainGuard.Optional(merchant, nameof(merchant), 160);
        AmountCents = DomainGuard.PositiveCents(amountCents, nameof(amountCents));
        TransactionType = transactionType;
        Frequency = frequency;
        DayOfMonth = dayOfMonth is null ? null : DomainGuard.Range(dayOfMonth.Value, nameof(dayOfMonth), 1, 31);
        DayOfWeek = dayOfWeek;
        NextOccurrenceDate = nextOccurrenceDate;
        EndDate = endDate;
        if (EndDate is not null && EndDate.Value < NextOccurrenceDate)
        {
            throw new DomainException("End date cannot be before next occurrence date.");
        }

        AutoCreate = autoCreate;
        UpdatedUtc = DomainGuard.Utc(updatedUtc, nameof(updatedUtc));
    }

    public void MoveNextOccurrence(DateOnly nextOccurrenceDate, DateTime updatedUtc)
    {
        if (EndDate is not null && EndDate.Value < nextOccurrenceDate)
        {
            throw new DomainException("Next occurrence cannot be after end date.");
        }

        NextOccurrenceDate = nextOccurrenceDate;
        UpdatedUtc = DomainGuard.Utc(updatedUtc, nameof(updatedUtc));
    }
}
