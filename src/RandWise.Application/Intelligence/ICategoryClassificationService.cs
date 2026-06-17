namespace RandWise.Application.Intelligence;

public interface ICategoryClassificationService
{
    Task<CategoryClassificationResult> ClassifyAsync(
        string userId,
        string description,
        string? merchant,
        CancellationToken cancellationToken);
}
