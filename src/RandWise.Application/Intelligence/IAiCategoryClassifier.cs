namespace RandWise.Application.Intelligence;

public interface IAiCategoryClassifier
{
    Task<AiClassificationResult?> ClassifyAsync(
        AiClassificationRequest request,
        CancellationToken cancellationToken);
}
