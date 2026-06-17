using RandWise.Domain.Common;
using RandWise.Domain.Enums;

namespace RandWise.Domain.Entities;

public sealed class IncomingMessage : Entity
{
    private IncomingMessage(
        string id,
        string? userId,
        string whatsAppMessageId,
        string platformContactId,
        string messageType,
        string? rawTextEncrypted,
        string payloadHash,
        DateTime receivedUtc)
        : base(id)
    {
        UserId = DomainGuard.Optional(userId, nameof(userId), 128);
        WhatsAppMessageId = DomainGuard.Required(whatsAppMessageId, nameof(whatsAppMessageId), 128);
        PlatformContactId = DomainGuard.Required(platformContactId, nameof(platformContactId), 128);
        MessageType = DomainGuard.Required(messageType, nameof(messageType), 64);
        RawTextEncrypted = DomainGuard.Optional(rawTextEncrypted, nameof(rawTextEncrypted));
        PayloadHash = DomainGuard.Required(payloadHash, nameof(payloadHash), 256);
        ProcessingStatus = MessageProcessingStatus.Received;
        ReceivedUtc = DomainGuard.Utc(receivedUtc, nameof(receivedUtc));
    }

    private IncomingMessage()
    {
        WhatsAppMessageId = string.Empty;
        PlatformContactId = string.Empty;
        MessageType = string.Empty;
        PayloadHash = string.Empty;
    }

    public string? UserId { get; private set; }
    public string WhatsAppMessageId { get; private set; }
    public string PlatformContactId { get; private set; }
    public string MessageType { get; private set; }
    public string? RawTextEncrypted { get; private set; }
    public string PayloadHash { get; private set; }
    public MessageProcessingStatus ProcessingStatus { get; private set; }
    public string? FailureReason { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTime ReceivedUtc { get; private set; }
    public DateTime? ProcessedUtc { get; private set; }

    public static IncomingMessage Create(
        string id,
        string? userId,
        string whatsAppMessageId,
        string platformContactId,
        string messageType,
        string? rawTextEncrypted,
        string payloadHash,
        DateTime receivedUtc) =>
        new(id, userId, whatsAppMessageId, platformContactId, messageType, rawTextEncrypted, payloadHash, receivedUtc);

    public void AssignUser(string userId)
    {
        UserId = DomainGuard.Required(userId, nameof(userId), 128);
    }

    public void MarkProcessed(DateTime processedUtc)
    {
        ProcessingStatus = MessageProcessingStatus.Processed;
        ProcessedUtc = DomainGuard.Utc(processedUtc, nameof(processedUtc));
        FailureReason = null;
    }

    public void RecordProcessingAttempt(DateTime attemptedUtc)
    {
        AttemptCount++;
        ProcessedUtc = DomainGuard.Utc(attemptedUtc, nameof(attemptedUtc));
    }

    public void MarkRetryableFailure(string failureReason)
    {
        ProcessingStatus = MessageProcessingStatus.Received;
        FailureReason = DomainGuard.Required(failureReason, nameof(failureReason), 500);
    }

    public void MarkFailed(string failureReason, DateTime processedUtc)
    {
        ProcessingStatus = MessageProcessingStatus.Failed;
        ProcessedUtc = DomainGuard.Utc(processedUtc, nameof(processedUtc));
        FailureReason = DomainGuard.Required(failureReason, nameof(failureReason), 500);
    }
}
