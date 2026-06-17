using RandWise.Contracts.RecurringTransactions;

namespace RandWise.Application.Budgeting;

public interface IRecurringTransactionService
{
    Task<IReadOnlyList<RecurringTransactionResponse>> ListAsync(string userId, CancellationToken cancellationToken);

    Task<RecurringTransactionResponse> CreateAsync(string userId, RecurringTransactionRequest request, CancellationToken cancellationToken);

    Task<RecurringTransactionResponse> UpdateAsync(string userId, string id, RecurringTransactionRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(string userId, string id, CancellationToken cancellationToken);

    Task<RecurringTransactionResponse> PauseAsync(string userId, string id, CancellationToken cancellationToken);
}
