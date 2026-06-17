namespace RandWise.Application.Audit;

public interface IAuditLogService
{
    Task RecordAsync(
        string? userId,
        string eventType,
        string? entityType,
        string? entityId,
        string? metadataJson,
        CancellationToken cancellationToken);
}
