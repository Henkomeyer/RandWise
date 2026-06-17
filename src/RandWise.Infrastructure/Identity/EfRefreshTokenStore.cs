using Microsoft.EntityFrameworkCore;
using RandWise.Application.Auth;
using RandWise.Infrastructure.Persistence;

namespace RandWise.Infrastructure.Identity;

public sealed class EfRefreshTokenStore : IRefreshTokenStore
{
    private readonly RandWiseDbContext dbContext;

    public EfRefreshTokenStore(RandWiseDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<RefreshTokenRecord?> FindActiveByHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var entity = await dbContext.RefreshTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        return entity is null || entity.RevokedUtc is not null || entity.ExpiresUtc <= now
            ? null
            : ToRecord(entity);
    }

    public async Task StoreAsync(RefreshTokenRecord refreshToken, CancellationToken cancellationToken)
    {
        dbContext.RefreshTokens.Add(ToEntity(refreshToken));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RotateAsync(
        string existingRefreshTokenId,
        RefreshTokenRecord replacementRefreshToken,
        DateTimeOffset revokedUtc,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var existing = await dbContext.RefreshTokens
            .SingleOrDefaultAsync(token => token.Id == existingRefreshTokenId, cancellationToken);

        if (existing is null || existing.RevokedUtc is not null || existing.ExpiresUtc <= revokedUtc)
        {
            throw new AuthFailureException(AuthFailure.InvalidRefreshToken, "Refresh token is invalid.");
        }

        var replacement = ToEntity(replacementRefreshToken);
        existing.RevokedUtc = revokedUtc;
        existing.ReplacedByTokenId = replacement.Id;
        dbContext.RefreshTokens.Add(replacement);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task RevokeAsync(string refreshTokenId, DateTimeOffset revokedUtc, CancellationToken cancellationToken)
    {
        var existing = await dbContext.RefreshTokens
            .SingleOrDefaultAsync(token => token.Id == refreshTokenId, cancellationToken);

        if (existing is null || existing.RevokedUtc is not null)
        {
            return;
        }

        existing.RevokedUtc = revokedUtc;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static RefreshTokenRecord ToRecord(RefreshTokenEntity entity) =>
        new(
            entity.Id,
            entity.UserId,
            entity.TokenHash,
            entity.CreatedUtc,
            entity.ExpiresUtc,
            entity.RevokedUtc,
            entity.ReplacedByTokenId);

    private static RefreshTokenEntity ToEntity(RefreshTokenRecord record) =>
        new()
        {
            Id = record.Id,
            UserId = record.UserId,
            TokenHash = record.TokenHash,
            CreatedUtc = record.CreatedUtc,
            ExpiresUtc = record.ExpiresUtc,
            RevokedUtc = record.RevokedUtc,
            ReplacedByTokenId = record.ReplacedByTokenId
        };
}
