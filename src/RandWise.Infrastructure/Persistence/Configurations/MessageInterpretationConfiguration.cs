using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RandWise.Domain.Entities;

namespace RandWise.Infrastructure.Persistence.Configurations;

public sealed class MessageInterpretationConfiguration : IEntityTypeConfiguration<MessageInterpretation>
{
    public void Configure(EntityTypeBuilder<MessageInterpretation> builder)
    {
        builder.ToTable("MessageInterpretations");

        builder.HasKey(interpretation => interpretation.Id);
        builder.Property(interpretation => interpretation.Id).HasMaxLength(128).ValueGeneratedNever();
        builder.Property(interpretation => interpretation.IncomingMessageId).HasMaxLength(128).IsRequired();
        builder.Property(interpretation => interpretation.Intent).HasMaxLength(80).IsRequired();
        builder.Property(interpretation => interpretation.AmountCents);
        builder.Property(interpretation => interpretation.TransactionType).HasConversion<int?>();
        builder.Property(interpretation => interpretation.Description).HasMaxLength(280);
        builder.Property(interpretation => interpretation.Merchant).HasMaxLength(160);
        builder.Property(interpretation => interpretation.TransactionDate).HasColumnType("TEXT");
        builder.Property(interpretation => interpretation.SuggestedCategoryId).HasMaxLength(128);
        builder.Property(interpretation => interpretation.ConfidenceBasisPoints).IsRequired();
        builder.Property(interpretation => interpretation.ParserVersion).HasMaxLength(64).IsRequired();
        builder.Property(interpretation => interpretation.RawStructuredResult);
        builder.Property(interpretation => interpretation.CreatedUtc).HasColumnType("TEXT").IsRequired();

        builder.HasIndex(interpretation => interpretation.IncomingMessageId).IsUnique();
        builder.HasOne<IncomingMessage>().WithOne().HasForeignKey<MessageInterpretation>(interpretation => interpretation.IncomingMessageId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<BudgetCategory>().WithMany().HasForeignKey(interpretation => interpretation.SuggestedCategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}
