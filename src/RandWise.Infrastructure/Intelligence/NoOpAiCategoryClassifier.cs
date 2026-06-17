using RandWise.Application.Intelligence;

namespace RandWise.Infrastructure.Intelligence;

public sealed class NoOpAiCategoryClassifier : IAiCategoryClassifier
{
    public Task<AiClassificationResult?> ClassifyAsync(
        AiClassificationRequest request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<AiClassificationResult?>(null);
    }
}
