using RandWise.Contracts.Profile;

namespace RandWise.Application.Privacy;

public interface IPrivacyService
{
    Task<ProfileExportResponse> ExportProfileAsync(string userId, CancellationToken cancellationToken);

    Task DeleteAccountAsync(string userId, CancellationToken cancellationToken);
}
