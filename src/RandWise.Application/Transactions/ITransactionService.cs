using RandWise.Contracts.Common;
using RandWise.Contracts.Transactions;

namespace RandWise.Application.Transactions;

public interface ITransactionService
{
    Task<TransactionResponse> CreateAsync(
        string userId,
        CreateTransactionRequest request,
        CancellationToken cancellationToken);

    Task<TransactionResponse> CreateFromWhatsAppAsync(
        string userId,
        string incomingMessageId,
        CreateTransactionRequest request,
        int confidenceBasisPoints,
        CancellationToken cancellationToken);

    Task<TransactionResponse> CreateFromRecurringAsync(
        string userId,
        string recurringTransactionId,
        CreateTransactionRequest request,
        CancellationToken cancellationToken);

    Task<PagedResponse<TransactionResponse>> ListAsync(
        string userId,
        TransactionQuery query,
        CancellationToken cancellationToken);

    Task<TransactionResponse?> GetAsync(string userId, string id, CancellationToken cancellationToken);

    Task<TransactionResponse> UpdateAsync(
        string userId,
        string id,
        UpdateTransactionRequest request,
        CancellationToken cancellationToken);

    Task DeleteAsync(string userId, string id, CancellationToken cancellationToken);

    Task<TransactionResponse> RestoreAsync(string userId, string id, CancellationToken cancellationToken);

    Task<TransactionResponse> CategoriseAsync(
        string userId,
        string id,
        CategoriseTransactionRequest request,
        CancellationToken cancellationToken);
}
