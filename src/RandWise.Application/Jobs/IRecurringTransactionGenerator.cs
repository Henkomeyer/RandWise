namespace RandWise.Application.Jobs;

public interface IRecurringTransactionGenerator
{
    Task<int> GenerateDueAsync(DateOnly today, CancellationToken cancellationToken);
}
