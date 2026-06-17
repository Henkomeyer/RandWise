using RandWise.Contracts.FinancialProfile;

namespace RandWise.Application.FinancialProfile;

public interface IFinancialProfileService
{
    Task<FinancialProfileResponse?> GetAsync(string userId, CancellationToken cancellationToken);

    Task<FinancialProfileResponse> UpsertAsync(
        string userId,
        FinancialProfileRequest request,
        CancellationToken cancellationToken);
}
