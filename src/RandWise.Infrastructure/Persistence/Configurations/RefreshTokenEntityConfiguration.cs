using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RandWise.Domain.Entities;
using RandWise.Infrastructure.Identity;

namespace RandWise.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenEntityConfiguration : IEntityTypeConfiguration<RefreshTokenEntity>
{
    public void Configure(EntityTypeBuilder<RefreshTokenEntity> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(token => token.Id);
        builder.Property(token => token.Id).HasMaxLength(128).ValueGeneratedNever();
        builder.Property(token => token.UserId).HasMaxLength(128).IsRequired();
        builder.Property(token => token.TokenHash).HasMaxLength(128).IsRequired();
        builder.Property(token => token.CreatedUtc).HasColumnType("TEXT").IsRequired();
        builder.Property(token => token.ExpiresUtc).HasColumnType("TEXT").IsRequired();
        builder.Property(token => token.RevokedUtc).HasColumnType("TEXT");
        builder.Property(token => token.ReplacedByTokenId).HasMaxLength(128);

        builder.HasIndex(token => token.TokenHash).IsUnique();
        builder.HasIndex(token => token.UserId);
        builder.HasIndex(token => token.ReplacedByTokenId);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}
