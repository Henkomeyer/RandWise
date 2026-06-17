using RandWise.Domain.Common;
using RandWise.Domain.Enums;

namespace RandWise.Domain.Entities;

public sealed class Transaction : UserOwnedAggregateRoot
{
    private Transaction(
        string id,
        string userId,
        string categoryId,
        long amountCents,
        TransactionType transactionType,
        string description,
        string? merchant,
        DateOnly transactionDate,
        TransactionSource source,
        TransactionStatus status,
        int? confidenceBasisPoints,
        DateTime createdUtc)
        : base(id, userId)
    {
        CategoryId = DomainGuard.Required(categoryId, nameof(categoryId), 128);
        AmountCents = DomainGuard.PositiveCents(amountCents, nameof(amountCents));
        TransactionType = transactionType;
        Description = DomainGuard.Required(description, nameof(description), 280);
        Merchant = DomainGuard.Optional(merchant, nameof(merchant), 160);
        TransactionDate = transactionDate;
        Source = source;
        Status = status;
        ConfidenceBasisPoints = confidenceBasisPoints is null ? null : DomainGuard.Range(confidenceBasisPoints.Value, nameof(confidenceBasisPoints), 0, 10000);
        CreatedUtc = DomainGuard.Utc(createdUtc, nameof(createdUtc));
        UpdatedUtc = CreatedUtc;
    }

    private Transaction()
    {
        CategoryId = string.Empty;
        Description = string.Empty;
    }

    public string CategoryId { get; private set; }
    public string? IncomingMessageId { get; private set; }
    public string? RecurringTransactionId { get; private set; }
    public long AmountCents { get; private set; }
    public TransactionType TransactionType { get; private set; }
    public string Description { get; private set; }
    public string? Merchant { get; private set; }
    public DateOnly TransactionDate { get; private set; }
    public TransactionSource Source { get; private set; }
    public TransactionStatus Status { get; private set; }
    public int? ConfidenceBasisPoints { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public DateTime UpdatedUtc { get; private set; }
    public DateTime? DeletedUtc { get; private set; }

    public static Transaction Create(
        string id,
        string userId,
        string categoryId,
        long amountCents,
        TransactionType transactionType,
        string description,
        string? merchant,
        DateOnly transactionDate,
        TransactionSource source,
        TransactionStatus status,
        int? confidenceBasisPoints,
        DateTime createdUtc) =>
        new(id, userId, categoryId, amountCents, transactionType, description, merchant, transactionDate, source, status, confidenceBasisPoints, createdUtc);

    public void LinkIncomingMessage(string incomingMessageId)
    {
        IncomingMessageId = DomainGuard.Required(incomingMessageId, nameof(incomingMessageId), 128);
    }

    public void LinkRecurringTransaction(string recurringTransactionId)
    {
        RecurringTransactionId = DomainGuard.Required(recurringTransactionId, nameof(recurringTransactionId), 128);
    }

    public void Update(
        string categoryId,
        long amountCents,
        TransactionType transactionType,
        string description,
        string? merchant,
        DateOnly transactionDate,
        string? notes,
        DateTime updatedUtc)
    {
        CategoryId = DomainGuard.Required(categoryId, nameof(categoryId), 128);
        AmountCents = DomainGuard.PositiveCents(amountCents, nameof(amountCents));
        TransactionType = transactionType;
        Description = DomainGuard.Required(description, nameof(description), 280);
        Merchant = DomainGuard.Optional(merchant, nameof(merchant), 160);
        TransactionDate = transactionDate;
        Notes = DomainGuard.Optional(notes, nameof(notes), 500);
        UpdatedUtc = DomainGuard.Utc(updatedUtc, nameof(updatedUtc));
    }

    public void MarkDeleted(DateTime deletedUtc)
    {
        DeletedUtc = DomainGuard.Utc(deletedUtc, nameof(deletedUtc));
        UpdatedUtc = DeletedUtc.Value;
        Status = TransactionStatus.Deleted;
    }

    public void Restore(DateTime restoredUtc)
    {
        DeletedUtc = null;
        UpdatedUtc = DomainGuard.Utc(restoredUtc, nameof(restoredUtc));
        Status = ConfidenceBasisPoints is >= 7000 and < 9000 ? TransactionStatus.NeedsReview : TransactionStatus.Confirmed;
    }
}
