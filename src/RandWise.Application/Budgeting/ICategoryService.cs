using RandWise.Contracts.Categories;

namespace RandWise.Application.Budgeting;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryResponse>> ListAsync(string userId, CancellationToken cancellationToken);

    Task<CategoryResponse> CreateAsync(string userId, CategoryRequest request, CancellationToken cancellationToken);

    Task<CategoryResponse> UpdateAsync(string userId, string id, CategoryRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(string userId, string id, CancellationToken cancellationToken);
}
