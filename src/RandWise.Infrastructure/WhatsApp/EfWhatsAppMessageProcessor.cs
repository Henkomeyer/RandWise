using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RandWise.Application.Common;
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
    private readonly RandWiseDbContext dbContext;
    private readonly IClock clock;
    private readonly IDeterministicWhatsAppParser parser;
    private readonly IIdGenerator idGenerator;
    private readonly ISensitiveDataProtector protector;
    private readonly ITransactionService transactionService;

    public EfWhatsAppMessageProcessor(
        RandWiseDbContext dbContext,
        IClock clock,
        IDeterministicWhatsAppParser parser,
        IIdGenerator idGenerator,
        ISensitiveDataProtector protector,
        ITransactionService transactionService)
    {
        this.dbContext = dbContext;
        this.clock = clock;
        this.parser = parser;
        this.idGenerator = idGenerator;
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
        var interpretation = MessageInterpretation.Create(
            idGenerator.NewId(),
            incoming.Id,
            parsed.Intent,
            parsed.AmountInCents,
            ParseTransactionType(parsed.TransactionType),
            parsed.Description,
            parsed.Merchant,
            parsed.TransactionDate,
            null,
            parsed.ConfidenceBasisPoints,
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

        await transactionService.CreateFromWhatsAppAsync(
            incoming.UserId,
            incoming.Id,
            new CreateTransactionRequest(
                parsed.AmountInCents.Value,
                parsed.TransactionType,
                null,
                parsed.Description,
                parsed.Merchant,
                parsed.TransactionDate.Value,
                "whatsapp"),
            cancellationToken);

        incoming.MarkProcessed(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static TransactionType? ParseTransactionType(string? transactionType) =>
        transactionType switch
        {
            "expense" => TransactionType.Expense,
            "income" => TransactionType.Income,
            _ => null
        };
}
