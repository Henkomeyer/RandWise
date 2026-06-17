using RandWise.Contracts.CategoryRules;

namespace RandWise.Application.Intelligence;

public interface ICategoryRuleService
{
    Task<IReadOnlyList<CategoryRuleResponse>> ListAsync(string userId, CancellationToken cancellationToken);

    Task<CategoryRuleResponse> CreateAsync(
        string userId,
        CategoryRuleRequest request,
        CancellationToken cancellationToken);

    Task DeactivateAsync(string userId, string id, CancellationToken cancellationToken);
}
