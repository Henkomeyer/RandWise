namespace RandWise.Application.Intelligence;

public sealed record CategoryClassificationResult(
    string? CategoryId,
    int ConfidenceBasisPoints,
    string Basis);
