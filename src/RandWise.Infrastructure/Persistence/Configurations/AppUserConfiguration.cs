using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RandWise.Domain.Entities;

namespace RandWise.Infrastructure.Persistence.Configurations;

public sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("AppUsers");

        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id).HasMaxLength(128).ValueGeneratedNever();
        builder.Property(user => user.IdentityUserId).HasMaxLength(128).IsRequired();
        builder.Property(user => user.DisplayName).HasMaxLength(160).IsRequired();
        builder.Property(user => user.PreferredCurrency).HasMaxLength(3).IsRequired().HasDefaultValue("ZAR");
        builder.Property(user => user.TimeZone).HasMaxLength(100).IsRequired().HasDefaultValue("Africa/Johannesburg");
        builder.Property(user => user.PreferredLanguage).HasMaxLength(16).IsRequired().HasDefaultValue("en-ZA");
        builder.Property(user => user.Status).HasConversion<int>().IsRequired();
        builder.Property(user => user.CreatedUtc).HasColumnType("TEXT").IsRequired();
        builder.Property(user => user.UpdatedUtc).HasColumnType("TEXT").IsRequired();
        builder.Property(user => user.DeletedUtc).HasColumnType("TEXT");

        builder.HasIndex(user => user.IdentityUserId).IsUnique();
    }
}
