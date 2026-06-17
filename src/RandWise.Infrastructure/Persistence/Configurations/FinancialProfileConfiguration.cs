using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RandWise.Domain.Entities;

namespace RandWise.Infrastructure.Persistence.Configurations;

public sealed class FinancialProfileConfiguration : IEntityTypeConfiguration<FinancialProfile>
{
    public void Configure(EntityTypeBuilder<FinancialProfile> builder)
    {
        builder.ToTable("FinancialProfiles");

        builder.HasKey(profile => profile.Id);
        builder.Property(profile => profile.Id).HasMaxLength(128).ValueGeneratedNever();
        builder.Property(profile => profile.UserId).HasMaxLength(128).IsRequired();
        builder.Property(profile => profile.DefaultMonthlyIncomeCents).IsRequired();
        builder.Property(profile => profile.PaydayDay);
        builder.Property(profile => profile.BudgetCycleType).HasConversion<int>().IsRequired();
        builder.Property(profile => profile.StartingBalanceCents).IsRequired().HasDefaultValue(0L);
        builder.Property(profile => profile.SafetyBufferCents).IsRequired().HasDefaultValue(0L);
        builder.Property(profile => profile.SavingsCommitmentCents).IsRequired().HasDefaultValue(0L);
        builder.Property(profile => profile.NotificationMode).HasConversion<int>().IsRequired();
        builder.Property(profile => profile.FirstDayOfWeek).HasConversion<int>().IsRequired();
        builder.Property(profile => profile.CreatedUtc).HasColumnType("TEXT").IsRequired();
        builder.Property(profile => profile.UpdatedUtc).HasColumnType("TEXT").IsRequired();

        builder.HasIndex(profile => profile.UserId).IsUnique();
        builder.HasOne<AppUser>().WithOne().HasForeignKey<FinancialProfile>(profile => profile.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
