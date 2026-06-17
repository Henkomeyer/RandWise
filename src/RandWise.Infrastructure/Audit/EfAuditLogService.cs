using RandWise.Application.Audit;
using RandWise.Application.Common;
using RandWise.Domain.Entities;
using RandWise.Infrastructure.Persistence;

namespace RandWise.Infrastructure.Audit;

public sealed class EfAuditLogService : IAuditLogService
{
    private readonly RandWiseDbContext dbContext;
    private readonly IClock clock;
    private readonly IIdGenerator idGenerator;

    public EfAuditLogService(RandWiseDbContext dbContext, IClock clock, IIdGenerator idGenerator)
    {
        this.dbContext = dbContext;
        this.clock = clock;
        this.idGenerator = idGenerator;
    }

    public async Task RecordAsync(
        string? userId,
        string eventType,
        string? entityType,
        string? entityId,
        string? metadataJson,
        CancellationToken cancellationToken)
    {
        dbContext.AuditLogs.Add(AuditLog.Create(
            idGenerator.NewId(),
            userId,
            eventType,
            entityType,
            entityId,
            metadataJson,
            ipAddressHash: null,
            clock.UtcNow));

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
