using RandWise.Domain.Common;
using RandWise.Domain.Enums;

namespace RandWise.Domain.Entities;

public sealed class MessageInterpretation : Entity
{
    private MessageInterpretation(
        string id,
        string incomingMessageId,
        string intent,
        long? amountCents,
        TransactionType? transactionType,
        string? description,
        string? merchant,
        DateOnly? transactionDate,
        string? suggestedCategoryId,
        int confidenceBasisPoints,
        string parserVersion,
        string? rawStructuredResult,
        DateTime createdUtc)
        : base(id)
    {
        IncomingMessageId = DomainGuard.Required(incomingMessageId, nameof(incomingMessageId), 128);
        Intent = DomainGuard.Required(intent, nameof(intent), 80);
        AmountCents = amountCents is null ? null : DomainGuard.PositiveCents(amountCents.Value, nameof(amountCents));
        TransactionType = transactionType;
        Description = DomainGuard.Optional(description, nameof(description), 280);
        Merchant = DomainGuard.Optional(merchant, nameof(merchant), 160);
        TransactionDate = transactionDate;
        SuggestedCategoryId = DomainGuard.Optional(suggestedCategoryId, nameof(suggestedCategoryId), 128);
        ConfidenceBasisPoints = DomainGuard.Range(confidenceBasisPoints, nameof(confidenceBasisPoints), 0, 10000);
        ParserVersion = DomainGuard.Required(parserVersion, nameof(parserVersion), 64);
        RawStructuredResult = DomainGuard.Optional(rawStructuredResult, nameof(rawStructuredResult));
        CreatedUtc = DomainGuard.Utc(createdUtc, nameof(createdUtc));
    }

    private MessageInterpretation()
    {
        IncomingMessageId = string.Empty;
        Intent = string.Empty;
        ParserVersion = string.Empty;
    }

    public string IncomingMessageId { get; private set; }
    public string Intent { get; private set; }
    public long? AmountCents { get; private set; }
    public TransactionType? TransactionType { get; private set; }
    public string? Description { get; private set; }
    public string? Merchant { get; private set; }
    public DateOnly? TransactionDate { get; private set; }
    public string? SuggestedCategoryId { get; private set; }
    public int ConfidenceBasisPoints { get; private set; }
    public string ParserVersion { get; private set; }
    public string? RawStructuredResult { get; private set; }
    public DateTime CreatedUtc { get; private set; }

    public static MessageInterpretation Create(
        string id,
        string incomingMessageId,
        string intent,
        long? amountCents,
        TransactionType? transactionType,
        string? description,
        string? merchant,
        DateOnly? transactionDate,
        string? suggestedCategoryId,
        int confidenceBasisPoints,
        string parserVersion,
        string? rawStructuredResult,
        DateTime createdUtc) =>
        new(id, incomingMessageId, intent, amountCents, transactionType, description, merchant, transactionDate, suggestedCategoryId, confidenceBasisPoints, parserVersion, rawStructuredResult, createdUtc);
}
