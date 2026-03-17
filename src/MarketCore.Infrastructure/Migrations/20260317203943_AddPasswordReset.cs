using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordReset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent MySQL SQL — safe to re-run if a previous attempt partially succeeded.
            // ADD COLUMN IF NOT EXISTS: MySQL 8.0+
            // CREATE UNIQUE INDEX IF NOT EXISTS: MySQL 8.0.29+
            // The original DropIndex+CreateIndex for EmailVerificationToken is replaced by
            // CREATE INDEX IF NOT EXISTS (no-op when it already exists, recreates when dropped).

            migrationBuilder.Sql(
                "ALTER TABLE `Users` " +
                "ADD COLUMN IF NOT EXISTS `PasswordResetToken` VARCHAR(128) NULL, " +
                "ADD COLUMN IF NOT EXISTS `PasswordResetTokenExpiresAt` DATETIME(6) NULL;");

            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX IF NOT EXISTS `IX_Users_EmailVerificationToken` " +
                "ON `Users` (`EmailVerificationToken`);");

            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX IF NOT EXISTS `IX_Users_PasswordResetToken` " +
                "ON `Users` (`PasswordResetToken`);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE `Users` " +
                "DROP COLUMN IF EXISTS `PasswordResetToken`, " +
                "DROP COLUMN IF EXISTS `PasswordResetTokenExpiresAt`;");
        }
    }
}
