using MarketCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketCore.Infrastructure.Configurations;

public sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("Reviews", t =>
        {
            t.HasCheckConstraint("CK_Reviews_Rating", "[Rating] >= 1 AND [Rating] <= 5");
        });

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .ValueGeneratedNever();

        builder.Property(r => r.ProductId)
            .IsRequired();

        builder.Property(r => r.UserId)
            .IsRequired();

        builder.HasIndex(r => new { r.ProductId, r.UserId })
            .IsUnique()
            .HasDatabaseName("IX_Reviews_ProductId_UserId");

        builder.Property(r => r.Rating)
            .IsRequired();

        builder.Property(r => r.Comment)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        builder.Property(r => r.UpdatedAt).IsRequired();
        builder.Property(r => r.CreatedBy).HasMaxLength(256).IsRequired();
    }
}
