using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using RandWise.Domain.Entities;
using RandWise.Infrastructure.Identity;

namespace RandWise.Infrastructure.Persistence;

public sealed class RandWiseDbContext : IdentityDbContext<RandWiseIdentityUser>
{
    public RandWiseDbContext(DbContextOptions<RandWiseDbContext> options)
        : base(options)
    {
    }

    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<FinancialProfile> FinancialProfiles => Set<FinancialProfile>();
    public DbSet<BudgetPeriod> BudgetPeriods => Set<BudgetPeriod>();
    public DbSet<BudgetCategory> BudgetCategories => Set<BudgetCategory>();
    public DbSet<CategoryBudget> CategoryBudgets => Set<CategoryBudget>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<RecurringTransaction> RecurringTransactions => Set<RecurringTransaction>();
    public DbSet<WhatsAppContact> WhatsAppContacts => Set<WhatsAppContact>();
    public DbSet<IncomingMessage> IncomingMessages => Set<IncomingMessage>();
    public DbSet<MessageInterpretation> MessageInterpretations => Set<MessageInterpretation>();
    public DbSet<UserCategoryRule> UserCategoryRules => Set<UserCategoryRule>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<RefreshTokenEntity> RefreshTokens => Set<RefreshTokenEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RandWiseDbContext).Assembly);
    }
}
