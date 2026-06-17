using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RandWise.Application.Audit;
using RandWise.Application.Common;
using RandWise.Application.Privacy;
using RandWise.Contracts.Profile;
using RandWise.Infrastructure.Identity;
using RandWise.Infrastructure.Persistence;
using AppException = RandWise.Application.Common.ApplicationException;

namespace RandWise.Infrastructure.Privacy;

public sealed class EfPrivacyService : IPrivacyService
{
    private readonly RandWiseDbContext dbContext;
    private readonly UserManager<RandWiseIdentityUser> userManager;
    private readonly IAuditLogService auditLogService;
    private readonly IClock clock;

    public EfPrivacyService(
        RandWiseDbContext dbContext,
        UserManager<RandWiseIdentityUser> userManager,
        IAuditLogService auditLogService,
        IClock clock)
    {
        this.dbContext = dbContext;
        this.userManager = userManager;
        this.auditLogService = auditLogService;
        this.clock = clock;
    }

    public async Task<ProfileExportResponse> ExportProfileAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.AppUsers
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.Id == userId && user.DeletedUtc == null, cancellationToken);

        if (user is null)
        {
            throw new AppException(ApplicationError.NotFound, "Profile was not found.");
        }

        var periodIds = await dbContext.BudgetPeriods
            .AsNoTracking()
            .Where(period => period.UserId == userId)
            .Select(period => period.Id)
            .ToListAsync(cancellationToken);

        var export = new ProfileExportResponse(
            clock.UtcNow,
            new ProfileExportUserResponse(
                user.Id,
                user.DisplayName,
                user.PreferredCurrency,
                user.TimeZone,
                user.PreferredLanguage,
                user.CreatedUtc),
            await dbContext.FinancialProfiles.AsNoTracking().Where(profile => profile.UserId == userId).Select(profile => new
            {
                profile.DefaultMonthlyIncomeCents,
                profile.PaydayDay,
                profile.BudgetCycleType,
                profile.StartingBalanceCents,
                profile.SafetyBufferCents,
                profile.SavingsCommitmentCents,
                profile.NotificationMode,
                profile.FirstDayOfWeek
            }).SingleOrDefaultAsync(cancellationToken),
            await dbContext.BudgetCategories.AsNoTracking().Where(category => category.UserId == userId || category.IsSystem).Select(category => new
            {
                category.Id,
                category.Name,
                category.Slug,
                category.CategoryType,
                category.SortOrder,
                category.IsSystem,
                category.IsActive
            }).ToListAsync<object>(cancellationToken),
            await dbContext.BudgetPeriods.AsNoTracking().Where(period => period.UserId == userId).Select(period => new
            {
                period.Id,
                period.StartDate,
                period.EndDate,
                period.ExpectedIncomeCents,
                period.ActualIncomeCents,
                period.OpeningBalanceCents,
                period.Status
            }).ToListAsync<object>(cancellationToken),
            await dbContext.CategoryBudgets.AsNoTracking().Where(budget => periodIds.Contains(budget.BudgetPeriodId)).Select(budget => new
            {
                budget.Id,
                budget.BudgetPeriodId,
                budget.CategoryId,
                budget.AllocatedAmountCents,
                budget.RolloverAmountCents,
                budget.WarningThresholdPercent
            }).ToListAsync<object>(cancellationToken),
            await dbContext.RecurringTransactions.AsNoTracking().Where(recurring => recurring.UserId == userId).Select(recurring => new
            {
                recurring.Id,
                recurring.CategoryId,
                recurring.Description,
                recurring.Merchant,
                recurring.AmountCents,
                recurring.TransactionType,
                recurring.Frequency,
                recurring.NextOccurrenceDate,
                recurring.EndDate,
                recurring.AutoCreate,
                recurring.IsActive
            }).ToListAsync<object>(cancellationToken),
            await dbContext.Transactions.AsNoTracking().Where(transaction => transaction.UserId == userId).Select(transaction => new
            {
                transaction.Id,
                transaction.CategoryId,
                transaction.AmountCents,
                transaction.TransactionType,
                transaction.Description,
                transaction.Merchant,
                transaction.TransactionDate,
                transaction.Source,
                transaction.Status,
                transaction.Notes,
                transaction.CreatedUtc,
                transaction.DeletedUtc
            }).ToListAsync<object>(cancellationToken),
            await dbContext.UserCategoryRules.AsNoTracking().Where(rule => rule.UserId == userId).Select(rule => new
            {
                rule.Id,
                rule.MatchType,
                rule.MatchValue,
                rule.CategoryId,
                rule.Priority,
                rule.IsActive
            }).ToListAsync<object>(cancellationToken),
            await dbContext.WhatsAppContacts.AsNoTracking().Where(contact => contact.UserId == userId).Select(contact => new
            {
                contact.Id,
                contact.IsVerified,
                contact.VerifiedUtc,
                contact.CreatedUtc,
                contact.UpdatedUtc
            }).ToListAsync<object>(cancellationToken));

        await auditLogService.RecordAsync(userId, "privacy.data_exported", "AppUser", userId, null, cancellationToken);
        return export;
    }

    public async Task DeleteAccountAsync(string userId, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var user = await dbContext.AppUsers.SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);
        if (user is null || user.DeletedUtc is not null)
        {
            throw new AppException(ApplicationError.NotFound, "Profile was not found.");
        }

        var periodIds = await dbContext.BudgetPeriods
            .Where(period => period.UserId == userId)
            .Select(period => period.Id)
            .ToListAsync(cancellationToken);
        var incomingIds = await dbContext.IncomingMessages
            .Where(message => message.UserId == userId)
            .Select(message => message.Id)
            .ToListAsync(cancellationToken);

        dbContext.Transactions.RemoveRange(dbContext.Transactions.Where(row => row.UserId == userId));
        dbContext.MessageInterpretations.RemoveRange(dbContext.MessageInterpretations.Where(row => incomingIds.Contains(row.IncomingMessageId)));
        dbContext.IncomingMessages.RemoveRange(dbContext.IncomingMessages.Where(row => row.UserId == userId));
        dbContext.Notifications.RemoveRange(dbContext.Notifications.Where(row => row.UserId == userId));
        dbContext.UserCategoryRules.RemoveRange(dbContext.UserCategoryRules.Where(row => row.UserId == userId));
        dbContext.WhatsAppContacts.RemoveRange(dbContext.WhatsAppContacts.Where(row => row.UserId == userId));
        dbContext.RecurringTransactions.RemoveRange(dbContext.RecurringTransactions.Where(row => row.UserId == userId));
        dbContext.CategoryBudgets.RemoveRange(dbContext.CategoryBudgets.Where(row => periodIds.Contains(row.BudgetPeriodId)));
        dbContext.BudgetPeriods.RemoveRange(dbContext.BudgetPeriods.Where(row => row.UserId == userId));
        dbContext.BudgetCategories.RemoveRange(dbContext.BudgetCategories.Where(row => row.UserId == userId));
        dbContext.FinancialProfiles.RemoveRange(dbContext.FinancialProfiles.Where(row => row.UserId == userId));
        dbContext.RefreshTokens.RemoveRange(dbContext.RefreshTokens.Where(row => row.UserId == userId));

        var identityUser = await userManager.FindByIdAsync(user.IdentityUserId);
        if (identityUser is not null)
        {
            var deletedEmail = $"deleted-{user.Id}@deleted.randwise.local";
            identityUser.Email = deletedEmail;
            identityUser.UserName = deletedEmail;
            identityUser.NormalizedEmail = userManager.NormalizeEmail(deletedEmail);
            identityUser.NormalizedUserName = userManager.NormalizeName(deletedEmail);
            identityUser.PhoneNumber = null;
            identityUser.EmailConfirmed = false;
            await userManager.UpdateAsync(identityUser);
        }

        user.AnonymizeAndMarkDeleted(clock.UtcNow);
        dbContext.AuditLogs.Add(RandWise.Domain.Entities.AuditLog.Create(
            Guid.NewGuid().ToString("N"),
            null,
            "privacy.account_deleted",
            "AppUser",
            userId,
            JsonSerializer.Serialize(new { deletedUserId = userId }),
            null,
            clock.UtcNow));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
