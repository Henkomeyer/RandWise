using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RandWise.Domain.Entities;

namespace RandWise.Infrastructure.Persistence.Configurations;

public sealed class CategoryBudgetConfiguration : IEntityTypeConfiguration<CategoryBudget>
{
    public void Configure(EntityTypeBuilder<CategoryBudget> builder)
    {
        builder.ToTable("CategoryBudgets");

        builder.HasKey(budget => budget.Id);
        builder.Property(budget => budget.Id).HasMaxLength(128).ValueGeneratedNever();
        builder.Property(budget => budget.BudgetPeriodId).HasMaxLength(128).IsRequired();
        builder.Property(budget => budget.CategoryId).HasMaxLength(128).IsRequired();
        builder.Property(budget => budget.AllocatedAmountCents).IsRequired();
        builder.Property(budget => budget.RolloverAmountCents).IsRequired().HasDefaultValue(0L);
        builder.Property(budget => budget.WarningThresholdPercent).IsRequired().HasDefaultValue(80);
        builder.Property(budget => budget.CreatedUtc).HasColumnType("TEXT").IsRequired();
        builder.Property(budget => budget.UpdatedUtc).HasColumnType("TEXT").IsRequired();

        builder.HasIndex(budget => new { budget.BudgetPeriodId, budget.CategoryId }).IsUnique();
        builder.HasOne<BudgetPeriod>().WithMany().HasForeignKey(budget => budget.BudgetPeriodId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<BudgetCategory>().WithMany().HasForeignKey(budget => budget.CategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}
