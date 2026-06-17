using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RandWise.Domain.Entities;

namespace RandWise.Infrastructure.Persistence.Configurations;

public sealed class BudgetCategoryConfiguration : IEntityTypeConfiguration<BudgetCategory>
{
    public void Configure(EntityTypeBuilder<BudgetCategory> builder)
    {
        builder.ToTable("BudgetCategories");

        builder.HasKey(category => category.Id);
        builder.Property(category => category.Id).HasMaxLength(128).ValueGeneratedNever();
        builder.Property(category => category.UserId).HasMaxLength(128);
        builder.Property(category => category.Name).HasMaxLength(100).IsRequired();
        builder.Property(category => category.Slug).HasMaxLength(120).IsRequired();
        builder.Property(category => category.CategoryType).HasConversion<int>().IsRequired();
        builder.Property(category => category.Icon).HasMaxLength(64);
        builder.Property(category => category.SortOrder).IsRequired();
        builder.Property(category => category.IsSystem).IsRequired();
        builder.Property(category => category.IsActive).IsRequired();
        builder.Property(category => category.CreatedUtc).HasColumnType("TEXT").IsRequired();
        builder.Property(category => category.UpdatedUtc).HasColumnType("TEXT").IsRequired();

        builder.HasIndex(category => new { category.UserId, category.Slug });
        builder.HasOne<AppUser>().WithMany().HasForeignKey(category => category.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
