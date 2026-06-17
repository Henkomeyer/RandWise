using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RandWise.Domain.Entities;

namespace RandWise.Infrastructure.Persistence.Configurations;

public sealed class WhatsAppContactConfiguration : IEntityTypeConfiguration<WhatsAppContact>
{
    public void Configure(EntityTypeBuilder<WhatsAppContact> builder)
    {
        builder.ToTable("WhatsAppContacts");

        builder.HasKey(contact => contact.Id);
        builder.Property(contact => contact.Id).HasMaxLength(128).ValueGeneratedNever();
        builder.Property(contact => contact.UserId).HasMaxLength(128).IsRequired();
        builder.Property(contact => contact.PhoneNumberHash).HasMaxLength(256).IsRequired();
        builder.Property(contact => contact.EncryptedPhoneNumber).IsRequired();
        builder.Property(contact => contact.PlatformContactId).HasMaxLength(128);
        builder.Property(contact => contact.IsVerified).IsRequired();
        builder.Property(contact => contact.VerifiedUtc).HasColumnType("TEXT");
        builder.Property(contact => contact.CreatedUtc).HasColumnType("TEXT").IsRequired();
        builder.Property(contact => contact.UpdatedUtc).HasColumnType("TEXT").IsRequired();

        builder.HasIndex(contact => contact.PhoneNumberHash).IsUnique();
        builder.HasIndex(contact => contact.UserId);
        builder.HasOne<AppUser>().WithMany().HasForeignKey(contact => contact.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
