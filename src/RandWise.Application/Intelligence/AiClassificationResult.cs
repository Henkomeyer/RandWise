namespace RandWise.Application.Intelligence;

public sealed record AiClassificationResult(
    string? CategoryName,
    int ConfidenceBasisPoints,
    string Provider);
