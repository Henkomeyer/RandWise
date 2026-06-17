using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RandWise.Domain.Entities;

namespace RandWise.Infrastructure.Persistence.Configurations;

public sealed class BudgetPeriodConfiguration : IEntityTypeConfiguration<BudgetPeriod>
{
    public void Configure(EntityTypeBuilder<BudgetPeriod> builder)
    {
        builder.ToTable("BudgetPeriods");

        builder.HasKey(period => period.Id);
        builder.Property(period => period.Id).HasMaxLength(128).ValueGeneratedNever();
        builder.Property(period => period.UserId).HasMaxLength(128).IsRequired();
        builder.Property(period => period.StartDate).HasColumnType("TEXT").IsRequired();
        builder.Property(period => period.EndDate).HasColumnType("TEXT").IsRequired();
        builder.Property(period => period.ExpectedIncomeCents).IsRequired();
        builder.Property(period => period.ActualIncomeCents).IsRequired().HasDefaultValue(0L);
        builder.Property(period => period.OpeningBalanceCents).IsRequired().HasDefaultValue(0L);
        builder.Property(period => period.Status).HasConversion<int>().IsRequired();
        builder.Property(period => period.CreatedUtc).HasColumnType("TEXT").IsRequired();
        builder.Property(period => period.UpdatedUtc).HasColumnType("TEXT").IsRequired();

        builder.HasIndex(period => new { period.UserId, period.StartDate, period.EndDate }).IsUnique();
        builder.HasOne<AppUser>().WithMany().HasForeignKey(period => period.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
