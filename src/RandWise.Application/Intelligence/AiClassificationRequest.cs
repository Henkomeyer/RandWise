namespace RandWise.Application.Intelligence;

public sealed record AiClassificationRequest(
    string Description,
    string? Merchant,
    IReadOnlyList<string> CandidateCategoryNames);
