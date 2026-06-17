using Microsoft.EntityFrameworkCore;
using RandWise.Application.Budgeting;
using RandWise.Application.Common;
using RandWise.Contracts.Budgeting;
using RandWise.Domain.Entities;
using RandWise.Infrastructure.Persistence;
using AppException = RandWise.Application.Common.ApplicationException;

namespace RandWise.Infrastructure.Budgeting;

public sealed class EfBudgetPeriodService : IBudgetPeriodService
{
    private readonly RandWiseDbContext dbContext;
    private readonly IClock clock;
    private readonly IIdGenerator idGenerator;

    public EfBudgetPeriodService(RandWiseDbContext dbContext, IClock clock, IIdGenerator idGenerator)
    {
        this.dbContext = dbContext;
        this.clock = clock;
        this.idGenerator = idGenerator;
    }

    public async Task<IReadOnlyList<BudgetPeriodResponse>> ListAsync(string userId, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(clock.UtcNow);
        return await dbContext.BudgetPeriods
            .AsNoTracking()
            .Where(period => period.UserId == userId)
            .OrderByDescending(period => period.StartDate)
            .Select(period => ToResponse(period, today))
            .ToListAsync(cancellationToken);
    }

    public async Task<BudgetPeriodResponse?> GetCurrentAsync(string userId, DateOnly today, CancellationToken cancellationToken)
    {
        var period = await dbContext.BudgetPeriods
            .AsNoTracking()
            .Where(period => period.UserId == userId
                && period.StartDate <= today
                && period.EndDate >= today)
            .OrderBy(period => period.StartDate)
            .FirstOrDefaultAsync(cancellationToken);

        return period is null ? null : ToResponse(period, today);
    }

    public async Task<BudgetPeriodResponse?> GetAsync(string userId, string id, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(clock.UtcNow);
        var period = await dbContext.BudgetPeriods
            .AsNoTracking()
            .SingleOrDefaultAsync(period => period.Id == id && period.UserId == userId, cancellationToken);

        return period is null ? null : ToResponse(period, today);
    }

    public async Task<BudgetPeriodResponse> CreateAsync(string userId, BudgetPeriodRequest request, CancellationToken cancellationToken)
    {
        await EnsureNoOverlapAsync(userId, request.StartDate, request.EndDate, excludeId: null, cancellationToken);
        var now = clock.UtcNow;
        var period = BudgetPeriod.Create(
            idGenerator.NewId(),
            userId,
            request.StartDate,
            request.EndDate,
            request.ExpectedIncomeCents,
            request.OpeningBalanceCents,
            now);

        dbContext.BudgetPeriods.Add(period);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(period, DateOnly.FromDateTime(now));
    }

    public async Task<BudgetPeriodResponse> UpdateAsync(
        string userId,
        string id,
        BudgetPeriodRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureNoOverlapAsync(userId, request.StartDate, request.EndDate, id, cancellationToken);
        var period = await GetOwnedPeriodAsync(userId, id, cancellationToken);
        period.UpdateDetails(
            request.StartDate,
            request.EndDate,
            request.ExpectedIncomeCents,
            request.OpeningBalanceCents,
            clock.UtcNow);

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(period, DateOnly.FromDateTime(clock.UtcNow));
    }

    public async Task<BudgetPeriodResponse> CloseAsync(string userId, string id, CancellationToken cancellationToken)
    {
        var period = await GetOwnedPeriodAsync(userId, id, cancellationToken);
        period.Close(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(period, DateOnly.FromDateTime(clock.UtcNow));
    }

    private async Task EnsureNoOverlapAsync(
        string userId,
        DateOnly startDate,
        DateOnly endDate,
        string? excludeId,
        CancellationToken cancellationToken)
    {
        var overlapExists = await dbContext.BudgetPeriods.AnyAsync(
            period => period.UserId == userId
                && period.Id != excludeId
                && period.StartDate <= endDate
                && period.EndDate >= startDate,
            cancellationToken);

        if (overlapExists)
        {
            throw new AppException(ApplicationError.Validation, "Budget period overlaps an existing period.");
        }
    }

    private async Task<BudgetPeriod> GetOwnedPeriodAsync(string userId, string id, CancellationToken cancellationToken)
    {
        var period = await dbContext.BudgetPeriods
            .SingleOrDefaultAsync(period => period.Id == id && period.UserId == userId, cancellationToken);

        return period ?? throw new AppException(ApplicationError.NotFound, "Budget period was not found.");
    }

    private static BudgetPeriodResponse ToResponse(BudgetPeriod period, DateOnly today) =>
        new(
            period.Id,
            period.StartDate,
            period.EndDate,
            period.ExpectedIncomeCents,
            period.ActualIncomeCents,
            period.OpeningBalanceCents,
            period.Status.ToContract(),
            period.DaysRemaining(today),
            period.CreatedUtc,
            period.UpdatedUtc);
}
