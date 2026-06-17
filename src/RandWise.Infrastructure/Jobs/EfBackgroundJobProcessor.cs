using Microsoft.EntityFrameworkCore;
using RandWise.Application.Common;
using RandWise.Application.Jobs;
using RandWise.Application.WhatsApp;
using RandWise.Domain.Enums;
using RandWise.Infrastructure.Persistence;

namespace RandWise.Infrastructure.Jobs;

public sealed class EfBackgroundJobProcessor : IBackgroundJobProcessor
{
    private const int MaxWhatsAppProcessingAttempts = 3;
    private readonly RandWiseDbContext dbContext;
    private readonly IClock clock;
    private readonly IRecurringTransactionGenerator recurringGenerator;
    private readonly IWhatsAppMessageProcessor whatsAppMessageProcessor;

    public EfBackgroundJobProcessor(
        RandWiseDbContext dbContext,
        IClock clock,
        IRecurringTransactionGenerator recurringGenerator,
        IWhatsAppMessageProcessor whatsAppMessageProcessor)
    {
        this.dbContext = dbContext;
        this.clock = clock;
        this.recurringGenerator = recurringGenerator;
        this.whatsAppMessageProcessor = whatsAppMessageProcessor;
    }

    public async Task<int> ProcessQueuedWhatsAppMessagesAsync(CancellationToken cancellationToken)
    {
        var exhaustedMessages = await dbContext.IncomingMessages
            .Where(message => message.ProcessingStatus == MessageProcessingStatus.Received
                && message.AttemptCount >= MaxWhatsAppProcessingAttempts)
            .ToListAsync(cancellationToken);

        foreach (var exhaustedMessage in exhaustedMessages)
        {
            exhaustedMessage.MarkFailed("Exceeded maximum WhatsApp processing attempts.", clock.UtcNow);
        }

        if (exhaustedMessages.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var messageIds = await dbContext.IncomingMessages
            .AsNoTracking()
            .Where(message => message.ProcessingStatus == MessageProcessingStatus.Received
                && message.AttemptCount < MaxWhatsAppProcessingAttempts)
            .OrderBy(message => message.ReceivedUtc)
            .Select(message => message.Id)
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var messageId in messageIds)
        {
            try
            {
                await whatsAppMessageProcessor.ProcessAsync(messageId, cancellationToken);
            }
            catch (Exception exception)
            {
                await RecordProcessingExceptionAsync(messageId, exception, cancellationToken);
            }
        }

        return messageIds.Count;
    }

    public async Task<int> GenerateDueRecurringTransactionsAsync(DateOnly today, CancellationToken cancellationToken) =>
        await recurringGenerator.GenerateDueAsync(today, cancellationToken);

    private async Task RecordProcessingExceptionAsync(
        string messageId,
        Exception exception,
        CancellationToken cancellationToken)
    {
        dbContext.ChangeTracker.Clear();

        var incoming = await dbContext.IncomingMessages
            .SingleOrDefaultAsync(message => message.Id == messageId, cancellationToken);

        if (incoming is null || incoming.ProcessingStatus != MessageProcessingStatus.Received)
        {
            return;
        }

        var failureReason = $"WhatsApp processing failed: {exception.GetType().Name}.";
        if (incoming.AttemptCount >= MaxWhatsAppProcessingAttempts)
        {
            incoming.MarkFailed(failureReason, clock.UtcNow);
        }
        else
        {
            incoming.MarkRetryableFailure(failureReason);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
