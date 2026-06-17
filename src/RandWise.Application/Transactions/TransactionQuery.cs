namespace RandWise.Application.Transactions;

public sealed record TransactionQuery(
    DateOnly? From,
    DateOnly? To,
    string? CategoryId,
    string? Type,
    string? Source,
    string? Search,
    int Page,
    int PageSize);
