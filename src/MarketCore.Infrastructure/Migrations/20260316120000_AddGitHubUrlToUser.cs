using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGitHubUrlToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GitHubUrl",
                table: "Users",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GitHubUrl",
                table: "Users");
        }
    }
}
