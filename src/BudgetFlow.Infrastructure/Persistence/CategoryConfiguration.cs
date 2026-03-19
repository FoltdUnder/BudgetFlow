using BudgetFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetFlow.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.Type)
            .IsRequired();

        builder.Property(x => x.IsDefault)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.HasOne(x => x.User)
            .WithMany(x => x.Categories)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.UserId, x.Name, x.Type })
            .HasFilter("\"UserId\" IS NOT NULL")
            .IsUnique();

        builder.HasIndex(x => new { x.Name, x.Type })
            .HasFilter("\"IsDefault\" = TRUE")
            .IsUnique();
    }
}
