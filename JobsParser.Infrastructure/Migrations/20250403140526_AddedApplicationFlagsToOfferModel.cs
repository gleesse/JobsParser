using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobsParser.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedApplicationFlagsToOfferModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsApplied",
                table: "Offers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShouldApply",
                table: "Offers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ApplicationAttempts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErrorMsg = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApplitedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OfferId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationAttempts_Offers_OfferId",
                        column: x => x.OfferId,
                        principalTable: "Offers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationAttempts_OfferId",
                table: "ApplicationAttempts",
                column: "OfferId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationAttempts");

            migrationBuilder.DropColumn(
                name: "IsApplied",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "ShouldApply",
                table: "Offers");
        }
    }
}
