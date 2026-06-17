namespace RandWise.Application.Jobs;

public interface IBackgroundJobProcessor
{
    Task<int> ProcessQueuedWhatsAppMessagesAsync(CancellationToken cancellationToken);

    Task<int> GenerateDueRecurringTransactionsAsync(DateOnly today, CancellationToken cancellationToken);
}
