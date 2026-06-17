using RandWise.Domain.Common;
using RandWise.Domain.Enums;

namespace RandWise.Domain.Entities;

public sealed class Notification : UserOwnedAggregateRoot
{
    private Notification(
        string id,
        string userId,
        NotificationChannel channel,
        NotificationType notificationType,
        string messageEncrypted,
        DateTime scheduledUtc,
        DateTime createdUtc)
        : base(id, userId)
    {
        Channel = channel;
        NotificationType = notificationType;
        MessageEncrypted = DomainGuard.Required(messageEncrypted, nameof(messageEncrypted));
        Status = NotificationStatus.Scheduled;
        ScheduledUtc = DomainGuard.Utc(scheduledUtc, nameof(scheduledUtc));
        CreatedUtc = DomainGuard.Utc(createdUtc, nameof(createdUtc));
    }

    private Notification()
    {
        MessageEncrypted = string.Empty;
    }

    public NotificationChannel Channel { get; private set; }
    public NotificationType NotificationType { get; private set; }
    public string MessageEncrypted { get; private set; }
    public NotificationStatus Status { get; private set; }
    public DateTime ScheduledUtc { get; private set; }
    public DateTime? SentUtc { get; private set; }
    public string? FailureReason { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTime CreatedUtc { get; private set; }

    public static Notification Create(
        string id,
        string userId,
        NotificationChannel channel,
        NotificationType notificationType,
        string messageEncrypted,
        DateTime scheduledUtc,
        DateTime createdUtc) =>
        new(id, userId, channel, notificationType, messageEncrypted, scheduledUtc, createdUtc);

    public void MarkSent(DateTime sentUtc)
    {
        SentUtc = DomainGuard.Utc(sentUtc, nameof(sentUtc));
        Status = NotificationStatus.Sent;
        FailureReason = null;
    }

    public void MarkFailed(string failureReason)
    {
        AttemptCount++;
        Status = NotificationStatus.Failed;
        FailureReason = DomainGuard.Required(failureReason, nameof(failureReason), 500);
    }
}
