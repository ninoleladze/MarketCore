using MarketCore.Domain.Entities;
using MarketCore.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketCore.Infrastructure.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .ValueGeneratedNever();

        builder.Property(u => u.Email)
            .HasConversion(
                email => email.Value,
                value => new Domain.ValueObjects.Email(value))
            .HasColumnName("Email")
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("IX_Users_Email");

        builder.Property(u => u.PasswordHash)
            .HasColumnName("PasswordHash")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(u => u.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.LastName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.Role)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.OwnsOne(u => u.Address, address =>
        {
            address.Property(a => a.Street)
                .HasColumnName("Address_Street")
                .HasMaxLength(200);

            address.Property(a => a.City)
                .HasColumnName("Address_City")
                .HasMaxLength(100);

            address.Property(a => a.State)
                .HasColumnName("Address_State")
                .HasMaxLength(100);

            address.Property(a => a.ZipCode)
                .HasColumnName("Address_ZipCode")
                .HasMaxLength(20);

            address.Property(a => a.Country)
                .HasColumnName("Address_Country")
                .HasMaxLength(100);
        });

        builder.Property(u => u.IsEmailVerified)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(u => u.EmailVerificationToken)
            .HasColumnName("EmailVerificationToken")
            .HasMaxLength(128)
            .IsRequired(false);

        builder.HasIndex(u => u.EmailVerificationToken)
            .IsUnique()
            .HasFilter("[EmailVerificationToken] IS NOT NULL")
            .HasDatabaseName("IX_Users_EmailVerificationToken");

        builder.Property(u => u.PasswordResetToken)
            .HasColumnName("PasswordResetToken")
            .HasMaxLength(128)
            .IsRequired(false);

        builder.Property(u => u.PasswordResetTokenExpiresAt)
            .HasColumnName("PasswordResetTokenExpiresAt")
            .IsRequired(false);

        builder.HasIndex(u => u.PasswordResetToken)
            .IsUnique()
            .HasFilter("[PasswordResetToken] IS NOT NULL")
            .HasDatabaseName("IX_Users_PasswordResetToken");

        builder.Property(u => u.CreatedAt).IsRequired();
        builder.Property(u => u.UpdatedAt).IsRequired();
        builder.Property(u => u.CreatedBy).HasMaxLength(256).IsRequired();

        builder.HasOne(u => u.Cart)
            .WithOne()
            .HasForeignKey<Domain.Entities.Cart>(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
