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
            // A previous failed deploy dropped IX_Users_EmailVerificationToken via DropIndex
            // (MySQL auto-commits DDL) but never added the columns before crashing.
            // The migration was therefore NOT recorded in __EFMigrationsHistory, so EF Core
            // retries it every deploy — but DropIndex now fails because the index is gone.
            //
            // Fix:
            //   1. Skip DropIndex entirely — use CREATE INDEX IF NOT EXISTS instead.
            //      MySQL 8.0.29+ supports IF NOT EXISTS on CREATE INDEX: no-op if it exists,
            //      recreates it if it was dropped by the earlier partial run.
            //   2. Plain ADD COLUMN (no IF NOT EXISTS — MySQL 8.0 doesn't support that).
            //      Safe because EF Core only runs this when it is NOT in __EFMigrationsHistory,
            //      which guarantees these columns have never been added.

            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX IF NOT EXISTS `IX_Users_EmailVerificationToken` " +
                "ON `Users` (`EmailVerificationToken`);");

            migrationBuilder.Sql(
                "ALTER TABLE `Users` " +
                "ADD COLUMN `PasswordResetToken` VARCHAR(128) NULL, " +
                "ADD COLUMN `PasswordResetTokenExpiresAt` DATETIME(6) NULL;");

            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX IF NOT EXISTS `IX_Users_PasswordResetToken` " +
                "ON `Users` (`PasswordResetToken`);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Users_PasswordResetToken", table: "Users");

            migrationBuilder.DropColumn(name: "PasswordResetToken", table: "Users");
            migrationBuilder.DropColumn(name: "PasswordResetTokenExpiresAt", table: "Users");
        }
    }
}
