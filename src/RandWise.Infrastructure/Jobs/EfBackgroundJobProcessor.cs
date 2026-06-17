using Microsoft.EntityFrameworkCore;
using RandWise.Application.Jobs;
using RandWise.Application.WhatsApp;
using RandWise.Domain.Enums;
using RandWise.Infrastructure.Persistence;

namespace RandWise.Infrastructure.Jobs;

public sealed class EfBackgroundJobProcessor : IBackgroundJobProcessor
{
    private readonly RandWiseDbContext dbContext;
    private readonly IRecurringTransactionGenerator recurringGenerator;
    private readonly IWhatsAppMessageProcessor whatsAppMessageProcessor;

    public EfBackgroundJobProcessor(
        RandWiseDbContext dbContext,
        IRecurringTransactionGenerator recurringGenerator,
        IWhatsAppMessageProcessor whatsAppMessageProcessor)
    {
        this.dbContext = dbContext;
        this.recurringGenerator = recurringGenerator;
        this.whatsAppMessageProcessor = whatsAppMessageProcessor;
    }

    public async Task<int> ProcessQueuedWhatsAppMessagesAsync(CancellationToken cancellationToken)
    {
        var messageIds = await dbContext.IncomingMessages
            .AsNoTracking()
            .Where(message => message.ProcessingStatus == MessageProcessingStatus.Received)
            .OrderBy(message => message.ReceivedUtc)
            .Select(message => message.Id)
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var messageId in messageIds)
        {
            await whatsAppMessageProcessor.ProcessAsync(messageId, cancellationToken);
        }

        return messageIds.Count;
    }

    public async Task<int> GenerateDueRecurringTransactionsAsync(DateOnly today, CancellationToken cancellationToken) =>
        await recurringGenerator.GenerateDueAsync(today, cancellationToken);
}
