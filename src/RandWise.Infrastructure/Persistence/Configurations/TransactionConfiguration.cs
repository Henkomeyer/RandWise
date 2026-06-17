using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RandWise.Domain.Entities;

namespace RandWise.Infrastructure.Persistence.Configurations;

public sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");

        builder.HasKey(transaction => transaction.Id);
        builder.Property(transaction => transaction.Id).HasMaxLength(128).ValueGeneratedNever();
        builder.Property(transaction => transaction.UserId).HasMaxLength(128).IsRequired();
        builder.Property(transaction => transaction.CategoryId).HasMaxLength(128).IsRequired();
        builder.Property(transaction => transaction.IncomingMessageId).HasMaxLength(128);
        builder.Property(transaction => transaction.RecurringTransactionId).HasMaxLength(128);
        builder.Property(transaction => transaction.AmountCents).IsRequired();
        builder.Property(transaction => transaction.TransactionType).HasConversion<int>().IsRequired();
        builder.Property(transaction => transaction.Description).HasMaxLength(280).IsRequired();
        builder.Property(transaction => transaction.Merchant).HasMaxLength(160);
        builder.Property(transaction => transaction.TransactionDate).HasColumnType("TEXT").IsRequired();
        builder.Property(transaction => transaction.Source).HasConversion<int>().IsRequired();
        builder.Property(transaction => transaction.Status).HasConversion<int>().IsRequired();
        builder.Property(transaction => transaction.ConfidenceBasisPoints);
        builder.Property(transaction => transaction.Notes).HasMaxLength(500);
        builder.Property(transaction => transaction.CreatedUtc).HasColumnType("TEXT").IsRequired();
        builder.Property(transaction => transaction.UpdatedUtc).HasColumnType("TEXT").IsRequired();
        builder.Property(transaction => transaction.DeletedUtc).HasColumnType("TEXT");

        builder.HasIndex(transaction => new { transaction.UserId, transaction.TransactionDate });
        builder.HasIndex(transaction => new { transaction.UserId, transaction.CategoryId, transaction.TransactionDate });
        builder.HasIndex(transaction => transaction.IncomingMessageId);
        builder.HasOne<AppUser>().WithMany().HasForeignKey(transaction => transaction.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<BudgetCategory>().WithMany().HasForeignKey(transaction => transaction.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<IncomingMessage>().WithMany().HasForeignKey(transaction => transaction.IncomingMessageId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RecurringTransaction>().WithMany().HasForeignKey(transaction => transaction.RecurringTransactionId).OnDelete(DeleteBehavior.Restrict);
    }
}
