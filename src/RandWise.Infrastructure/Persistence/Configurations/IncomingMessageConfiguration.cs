using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RandWise.Domain.Entities;

namespace RandWise.Infrastructure.Persistence.Configurations;

public sealed class IncomingMessageConfiguration : IEntityTypeConfiguration<IncomingMessage>
{
    public void Configure(EntityTypeBuilder<IncomingMessage> builder)
    {
        builder.ToTable("IncomingMessages");

        builder.HasKey(message => message.Id);
        builder.Property(message => message.Id).HasMaxLength(128).ValueGeneratedNever();
        builder.Property(message => message.UserId).HasMaxLength(128);
        builder.Property(message => message.WhatsAppMessageId).HasMaxLength(128).IsRequired();
        builder.Property(message => message.PlatformContactId).HasMaxLength(128).IsRequired();
        builder.Property(message => message.MessageType).HasMaxLength(64).IsRequired();
        builder.Property(message => message.RawTextEncrypted);
        builder.Property(message => message.PayloadHash).HasMaxLength(256).IsRequired();
        builder.Property(message => message.ProcessingStatus).HasConversion<int>().IsRequired();
        builder.Property(message => message.FailureReason).HasMaxLength(500);
        builder.Property(message => message.AttemptCount).IsRequired().HasDefaultValue(0);
        builder.Property(message => message.ReceivedUtc).HasColumnType("TEXT").IsRequired();
        builder.Property(message => message.ProcessedUtc).HasColumnType("TEXT");

        builder.HasIndex(message => message.WhatsAppMessageId).IsUnique();
        builder.HasIndex(message => message.UserId);
        builder.HasOne<AppUser>().WithMany().HasForeignKey(message => message.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
