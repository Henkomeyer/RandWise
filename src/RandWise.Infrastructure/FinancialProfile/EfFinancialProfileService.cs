using Microsoft.EntityFrameworkCore;
using RandWise.Application.Common;
using RandWise.Application.FinancialProfile;
using RandWise.Contracts.FinancialProfile;
using RandWise.Domain.Entities;
using RandWise.Infrastructure.Persistence;

namespace RandWise.Infrastructure.FinancialProfiles;

public sealed class EfFinancialProfileService : IFinancialProfileService
{
    private readonly RandWiseDbContext dbContext;
    private readonly IClock clock;
    private readonly IIdGenerator idGenerator;

    public EfFinancialProfileService(RandWiseDbContext dbContext, IClock clock, IIdGenerator idGenerator)
    {
        this.dbContext = dbContext;
        this.clock = clock;
        this.idGenerator = idGenerator;
    }

    public async Task<FinancialProfileResponse?> GetAsync(string userId, CancellationToken cancellationToken)
    {
        var profile = await dbContext.FinancialProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(profile => profile.UserId == userId, cancellationToken);

        return profile is null ? null : ToResponse(profile);
    }

    public async Task<FinancialProfileResponse> UpsertAsync(
        string userId,
        FinancialProfileRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await dbContext.FinancialProfiles
            .SingleOrDefaultAsync(profile => profile.UserId == userId, cancellationToken);

        if (profile is null)
        {
            profile = FinancialProfile.Create(idGenerator.NewId(), userId, clock.UtcNow);
            dbContext.FinancialProfiles.Add(profile);
        }

        profile.Configure(
            request.DefaultMonthlyIncomeCents,
            request.PaydayDay,
            DomainEnumNames.ParseBudgetCycleType(request.BudgetCycleType),
            request.StartingBalanceCents,
            request.SafetyBufferCents,
            request.SavingsCommitmentCents,
            DomainEnumNames.ParseNotificationMode(request.NotificationMode),
            DomainEnumNames.ParseDayOfWeek(request.FirstDayOfWeek),
            clock.UtcNow);

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(profile);
    }

    private static FinancialProfileResponse ToResponse(Domain.Entities.FinancialProfile profile) =>
        new(
            profile.Id,
            profile.DefaultMonthlyIncomeCents,
            profile.PaydayDay,
            profile.BudgetCycleType.ToContract(),
            profile.StartingBalanceCents,
            profile.SafetyBufferCents,
            profile.SavingsCommitmentCents,
            profile.NotificationMode.ToContract(),
            profile.FirstDayOfWeek.ToContract(),
            profile.CreatedUtc,
            profile.UpdatedUtc);
}
