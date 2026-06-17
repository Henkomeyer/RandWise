using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RandWise.Domain.Entities;

namespace RandWise.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(log => log.Id);
        builder.Property(log => log.Id).HasMaxLength(128).ValueGeneratedNever();
        builder.Property(log => log.UserId).HasMaxLength(128);
        builder.Property(log => log.EventType).HasMaxLength(160).IsRequired();
        builder.Property(log => log.EntityType).HasMaxLength(160);
        builder.Property(log => log.EntityId).HasMaxLength(128);
        builder.Property(log => log.MetadataJson);
        builder.Property(log => log.IpAddressHash).HasMaxLength(256);
        builder.Property(log => log.CreatedUtc).HasColumnType("TEXT").IsRequired();

        builder.HasIndex(log => new { log.UserId, log.CreatedUtc });
        builder.HasOne<AppUser>().WithMany().HasForeignKey(log => log.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
