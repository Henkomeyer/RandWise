using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RandWise.Domain.Entities;

namespace RandWise.Infrastructure.Persistence.Configurations;

public sealed class RecurringTransactionConfiguration : IEntityTypeConfiguration<RecurringTransaction>
{
    public void Configure(EntityTypeBuilder<RecurringTransaction> builder)
    {
        builder.ToTable("RecurringTransactions");

        builder.HasKey(recurring => recurring.Id);
        builder.Property(recurring => recurring.Id).HasMaxLength(128).ValueGeneratedNever();
        builder.Property(recurring => recurring.UserId).HasMaxLength(128).IsRequired();
        builder.Property(recurring => recurring.CategoryId).HasMaxLength(128).IsRequired();
        builder.Property(recurring => recurring.Description).HasMaxLength(280).IsRequired();
        builder.Property(recurring => recurring.Merchant).HasMaxLength(160);
        builder.Property(recurring => recurring.AmountCents).IsRequired();
        builder.Property(recurring => recurring.TransactionType).HasConversion<int>().IsRequired();
        builder.Property(recurring => recurring.Frequency).HasConversion<int>().IsRequired();
        builder.Property(recurring => recurring.DayOfMonth);
        builder.Property(recurring => recurring.DayOfWeek).HasConversion<int?>();
        builder.Property(recurring => recurring.NextOccurrenceDate).HasColumnType("TEXT").IsRequired();
        builder.Property(recurring => recurring.EndDate).HasColumnType("TEXT");
        builder.Property(recurring => recurring.AutoCreate).IsRequired();
        builder.Property(recurring => recurring.IsActive).IsRequired();
        builder.Property(recurring => recurring.CreatedUtc).HasColumnType("TEXT").IsRequired();
        builder.Property(recurring => recurring.UpdatedUtc).HasColumnType("TEXT").IsRequired();

        builder.HasIndex(recurring => new { recurring.UserId, recurring.NextOccurrenceDate });
        builder.HasOne<AppUser>().WithMany().HasForeignKey(recurring => recurring.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<BudgetCategory>().WithMany().HasForeignKey(recurring => recurring.CategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}
