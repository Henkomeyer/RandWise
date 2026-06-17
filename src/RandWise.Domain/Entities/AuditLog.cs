using RandWise.Domain.Common;

namespace RandWise.Domain.Entities;

public sealed class AuditLog : Entity
{
    private AuditLog(
        string id,
        string? userId,
        string eventType,
        string? entityType,
        string? entityId,
        string? metadataJson,
        string? ipAddressHash,
        DateTime createdUtc)
        : base(id)
    {
        UserId = DomainGuard.Optional(userId, nameof(userId), 128);
        EventType = DomainGuard.Required(eventType, nameof(eventType), 160);
        EntityType = DomainGuard.Optional(entityType, nameof(entityType), 160);
        EntityId = DomainGuard.Optional(entityId, nameof(entityId), 128);
        MetadataJson = DomainGuard.Optional(metadataJson, nameof(metadataJson));
        IpAddressHash = DomainGuard.Optional(ipAddressHash, nameof(ipAddressHash), 256);
        CreatedUtc = DomainGuard.Utc(createdUtc, nameof(createdUtc));
    }

    private AuditLog()
    {
        EventType = string.Empty;
    }

    public string? UserId { get; private set; }
    public string EventType { get; private set; }
    public string? EntityType { get; private set; }
    public string? EntityId { get; private set; }
    public string? MetadataJson { get; private set; }
    public string? IpAddressHash { get; private set; }
    public DateTime CreatedUtc { get; private set; }

    public static AuditLog Create(
        string id,
        string? userId,
        string eventType,
        string? entityType,
        string? entityId,
        string? metadataJson,
        string? ipAddressHash,
        DateTime createdUtc) =>
        new(id, userId, eventType, entityType, entityId, metadataJson, ipAddressHash, createdUtc);
}
