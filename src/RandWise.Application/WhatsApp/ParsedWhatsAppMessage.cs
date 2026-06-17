namespace RandWise.Application.WhatsApp;

public sealed record ParsedWhatsAppMessage(
    string Intent,
    long? AmountInCents,
    string? TransactionType,
    string? Description,
    string? Merchant,
    DateOnly? TransactionDate,
    int ConfidenceBasisPoints,
    string ParserVersion);
