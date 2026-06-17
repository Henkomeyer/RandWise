namespace RandWise.Contracts.Categories;

public sealed record CategoryResponse(
    string Id,
    string Name,
    string Slug,
    string CategoryType,
    string? Icon,
    int SortOrder,
    bool IsSystem,
    bool IsActive,
    DateTime CreatedUtc,
    DateTime UpdatedUtc);
