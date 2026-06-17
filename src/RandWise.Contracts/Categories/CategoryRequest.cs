namespace RandWise.Contracts.Categories;

public sealed record CategoryRequest(
    string Name,
    string CategoryType,
    string? Icon,
    int SortOrder);
