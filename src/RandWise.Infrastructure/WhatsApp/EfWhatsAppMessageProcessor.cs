using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RandWise.Application.Common;
using RandWise.Application.Intelligence;
using RandWise.Application.Security;
using RandWise.Application.Transactions;
using RandWise.Application.WhatsApp;
using RandWise.Contracts.Transactions;
using RandWise.Domain.Entities;
using RandWise.Domain.Enums;
using RandWise.Infrastructure.Persistence;

namespace RandWise.Infrastructure.WhatsApp;

public sealed class EfWhatsAppMessageProcessor : IWhatsAppMessageProcessor
{
    private const int MinimumTransactionConfidenceBasisPoints = 7000;
    private readonly RandWiseDbContext dbContext;
    private readonly IClock clock;
    private readonly ICategoryClassificationService categoryClassificationService;
    private readonly IDeterministicWhatsAppParser parser;
    private readonly IIdGenerator idGenerator;
    private readonly IWhatsAppOutboundClient outboundClient;
    private readonly ISensitiveDataProtector protector;
    private readonly ITransactionService transactionService;

    public EfWhatsAppMessageProcessor(
        RandWiseDbContext dbContext,
        IClock clock,
        ICategoryClassificationService categoryClassificationService,
        IDeterministicWhatsAppParser parser,
        IIdGenerator idGenerator,
        IWhatsAppOutboundClient outboundClient,
        ISensitiveDataProtector protector,
        ITransactionService transactionService)
    {
        this.dbContext = dbContext;
        this.clock = clock;
        this.categoryClassificationService = categoryClassificationService;
        this.parser = parser;
        this.idGenerator = idGenerator;
        this.outboundClient = outboundClient;
        this.protector = protector;
        this.transactionService = transactionService;
    }

    public async Task ProcessAsync(string incomingMessageId, CancellationToken cancellationToken)
    {
        var incoming = await dbContext.IncomingMessages
            .SingleOrDefaultAsync(message => message.Id == incomingMessageId, cancellationToken);

        if (incoming is null || incoming.ProcessingStatus != MessageProcessingStatus.Received)
        {
            return;
        }

        incoming.RecordProcessingAttempt(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (incoming.UserId is null)
        {
            incoming.MarkFailed("WhatsApp contact is not linked.", clock.UtcNow);
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        if (string.IsNullOrWhiteSpace(incoming.RawTextEncrypted))
        {
            incoming.MarkFailed("Message has no text content.", clock.UtcNow);
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var text = protector.Unprotect(incoming.RawTextEncrypted);
        var parsed = parser.Parse(text, DateOnly.FromDateTime(incoming.ReceivedUtc));
        CategoryClassificationResult? classification = null;
        if (parsed.Intent == "create-transaction" && parsed.Description is not null)
        {
            classification = await categoryClassificationService.ClassifyAsync(
                incoming.UserId!,
                parsed.Description,
                parsed.Merchant,
                cancellationToken);
        }

        var interpretation = MessageInterpretation.Create(
            idGenerator.NewId(),
            incoming.Id,
            parsed.Intent,
            parsed.AmountInCents,
            ParseTransactionType(parsed.TransactionType),
            parsed.Description,
            parsed.Merchant,
            parsed.TransactionDate,
            classification?.CategoryId,
            Math.Min(parsed.ConfidenceBasisPoints, classification?.ConfidenceBasisPoints ?? parsed.ConfidenceBasisPoints),
            parsed.ParserVersion,
            JsonSerializer.Serialize(parsed),
            clock.UtcNow);

        dbContext.MessageInterpretations.Add(interpretation);

        if (parsed.Intent != "create-transaction"
            || parsed.AmountInCents is null
            || parsed.TransactionType is null
            || parsed.Description is null
            || parsed.TransactionDate is null)
        {
            incoming.MarkFailed("Message could not be interpreted as a transaction.", clock.UtcNow);
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        if (interpretation.ConfidenceBasisPoints < MinimumTransactionConfidenceBasisPoints)
        {
            await QueueClarificationAsync(incoming, parsed, cancellationToken);
            incoming.MarkFailed("Message confidence was below the transaction threshold.", clock.UtcNow);
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        await transactionService.CreateFromWhatsAppAsync(
            incoming.UserId,
            incoming.Id,
            new CreateTransactionRequest(
                parsed.AmountInCents.Value,
                parsed.TransactionType,
                classification?.CategoryId,
                parsed.Description,
                parsed.Merchant,
                parsed.TransactionDate.Value,
                "whatsapp"),
            interpretation.ConfidenceBasisPoints,
            cancellationToken);

        await QueueConfirmationAsync(incoming, parsed, cancellationToken);
        incoming.MarkProcessed(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task QueueConfirmationAsync(
        IncomingMessage incoming,
        ParsedWhatsAppMessage parsed,
        CancellationToken cancellationToken)
    {
        var notificationMode = await dbContext.FinancialProfiles
            .AsNoTracking()
            .Where(profile => profile.UserId == incoming.UserId)
            .Select(profile => profile.NotificationMode)
            .SingleOrDefaultAsync(cancellationToken);

        if (notificationMode == NotificationMode.Silent)
        {
            return;
        }

        if (notificationMode == default)
        {
            notificationMode = NotificationMode.Confirm;
        }

        var message = notificationMode == NotificationMode.Coach
            ? $"Added {FormatRand(parsed.AmountInCents!.Value)} for {parsed.Description}. Check your dashboard before the next spend."
            : $"Added {FormatRand(parsed.AmountInCents!.Value)} for {parsed.Description}.";

        var notification = Notification.Create(
            idGenerator.NewId(),
            incoming.UserId!,
            NotificationChannel.WhatsApp,
            NotificationType.TransactionConfirmation,
            protector.Protect(message),
            clock.UtcNow,
            clock.UtcNow);

        dbContext.Notifications.Add(notification);
        await outboundClient.SendTextAsync(incoming.PlatformContactId, message, cancellationToken);
        notification.MarkSent(clock.UtcNow);
    }

    private async Task QueueClarificationAsync(
        IncomingMessage incoming,
        ParsedWhatsAppMessage parsed,
        CancellationToken cancellationToken)
    {
        var message = parsed.AmountInCents is null
            ? "I could not read that spend. Please send it like: R250 petrol."
            : $"I am not sure where to put {FormatRand(parsed.AmountInCents.Value)}. Please reply with a clearer category.";

        var notification = Notification.Create(
            idGenerator.NewId(),
            incoming.UserId!,
            NotificationChannel.WhatsApp,
            NotificationType.ClarificationRequest,
            protector.Protect(message),
            clock.UtcNow,
            clock.UtcNow);

        dbContext.Notifications.Add(notification);
        await outboundClient.SendTextAsync(incoming.PlatformContactId, message, cancellationToken);
        notification.MarkSent(clock.UtcNow);
    }

    private static TransactionType? ParseTransactionType(string? transactionType) =>
        transactionType switch
        {
            "expense" => TransactionType.Expense,
            "income" => TransactionType.Income,
            _ => null
        };

    private static string FormatRand(long cents)
    {
        var rand = cents / 100m;
        return rand % 1 == 0 ? $"R{rand:0}" : $"R{rand:0.00}";
    }
}
