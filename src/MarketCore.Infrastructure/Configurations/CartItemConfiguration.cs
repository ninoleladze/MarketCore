using MarketCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketCore.Infrastructure.Configurations;

public sealed class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable("CartItems", t =>
        {
            t.HasCheckConstraint("CK_CartItems_Quantity", "[Quantity] >= 1");
        });

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .ValueGeneratedNever();

        builder.Property(i => i.CartId)
            .IsRequired();

        builder.Property(i => i.ProductId)
            .IsRequired();

        builder.HasIndex(i => new { i.CartId, i.ProductId })
            .IsUnique()
            .HasDatabaseName("IX_CartItems_CartId_ProductId");

        builder.Property(i => i.Quantity)
            .IsRequired();

        builder.OwnsOne(i => i.UnitPrice, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("UnitPrice_Amount")
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("UnitPrice_Currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(i => i.CreatedAt).IsRequired();
        builder.Property(i => i.UpdatedAt).IsRequired();
        builder.Property(i => i.CreatedBy).HasMaxLength(256).IsRequired();
    }
}
