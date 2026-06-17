using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RandWise.Domain.Entities;

namespace RandWise.Infrastructure.Persistence.Configurations;

public sealed class UserCategoryRuleConfiguration : IEntityTypeConfiguration<UserCategoryRule>
{
    public void Configure(EntityTypeBuilder<UserCategoryRule> builder)
    {
        builder.ToTable("UserCategoryRules");

        builder.HasKey(rule => rule.Id);
        builder.Property(rule => rule.Id).HasMaxLength(128).ValueGeneratedNever();
        builder.Property(rule => rule.UserId).HasMaxLength(128).IsRequired();
        builder.Property(rule => rule.MatchType).HasConversion<int>().IsRequired();
        builder.Property(rule => rule.MatchValue).HasMaxLength(160).IsRequired();
        builder.Property(rule => rule.NormalizedMatchValue).HasMaxLength(160).IsRequired();
        builder.Property(rule => rule.CategoryId).HasMaxLength(128).IsRequired();
        builder.Property(rule => rule.Priority).IsRequired();
        builder.Property(rule => rule.IsActive).IsRequired();
        builder.Property(rule => rule.CreatedUtc).HasColumnType("TEXT").IsRequired();
        builder.Property(rule => rule.UpdatedUtc).HasColumnType("TEXT").IsRequired();

        builder.HasIndex(rule => new { rule.UserId, rule.MatchType, rule.NormalizedMatchValue });
        builder.HasOne<AppUser>().WithMany().HasForeignKey(rule => rule.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<BudgetCategory>().WithMany().HasForeignKey(rule => rule.CategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}
