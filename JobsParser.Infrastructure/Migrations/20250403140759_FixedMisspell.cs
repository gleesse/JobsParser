using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobsParser.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixedMisspell : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ApplitedAt",
                table: "ApplicationAttempts",
                newName: "AppliedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AppliedAt",
                table: "ApplicationAttempts",
                newName: "ApplitedAt");
        }
    }
}
