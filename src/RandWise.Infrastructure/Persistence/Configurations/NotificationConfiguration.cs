using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RandWise.Domain.Entities;

namespace RandWise.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(notification => notification.Id);
        builder.Property(notification => notification.Id).HasMaxLength(128).ValueGeneratedNever();
        builder.Property(notification => notification.UserId).HasMaxLength(128).IsRequired();
        builder.Property(notification => notification.Channel).HasConversion<int>().IsRequired();
        builder.Property(notification => notification.NotificationType).HasConversion<int>().IsRequired();
        builder.Property(notification => notification.MessageEncrypted).IsRequired();
        builder.Property(notification => notification.Status).HasConversion<int>().IsRequired();
        builder.Property(notification => notification.ScheduledUtc).HasColumnType("TEXT").IsRequired();
        builder.Property(notification => notification.SentUtc).HasColumnType("TEXT");
        builder.Property(notification => notification.FailureReason).HasMaxLength(500);
        builder.Property(notification => notification.AttemptCount).IsRequired().HasDefaultValue(0);
        builder.Property(notification => notification.CreatedUtc).HasColumnType("TEXT").IsRequired();

        builder.HasIndex(notification => new { notification.UserId, notification.ScheduledUtc });
        builder.HasOne<AppUser>().WithMany().HasForeignKey(notification => notification.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
