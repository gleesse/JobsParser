using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobsParser.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangedOfferModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OfferDtoTechnology_Technology_TechnologiesId",
                table: "OfferDtoTechnology");

            migrationBuilder.DropForeignKey(
                name: "FK_Offers_Employer_EmployerId",
                table: "Offers");

            migrationBuilder.DropForeignKey(
                name: "FK_Offers_PositionLevel_PositionLevelId",
                table: "Offers");

            migrationBuilder.DropForeignKey(
                name: "FK_Offers_WorkMode_WorkModeId",
                table: "Offers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WorkMode",
                table: "WorkMode");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Technology",
                table: "Technology");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PositionLevel",
                table: "PositionLevel");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Employer",
                table: "Employer");

            migrationBuilder.RenameTable(
                name: "WorkMode",
                newName: "WorkModes");

            migrationBuilder.RenameTable(
                name: "Technology",
                newName: "Technologies");

            migrationBuilder.RenameTable(
                name: "PositionLevel",
                newName: "PositionLevels");

            migrationBuilder.RenameTable(
                name: "Employer",
                newName: "Employers");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Offers",
                newName: "Responsibilities");

            migrationBuilder.AddColumn<string>(
                name: "Requirements",
                table: "Offers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_WorkModes",
                table: "WorkModes",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Technologies",
                table: "Technologies",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PositionLevels",
                table: "PositionLevels",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Employers",
                table: "Employers",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OfferDtoTechnology_Technologies_TechnologiesId",
                table: "OfferDtoTechnology",
                column: "TechnologiesId",
                principalTable: "Technologies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Offers_Employers_EmployerId",
                table: "Offers",
                column: "EmployerId",
                principalTable: "Employers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Offers_PositionLevels_PositionLevelId",
                table: "Offers",
                column: "PositionLevelId",
                principalTable: "PositionLevels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Offers_WorkModes_WorkModeId",
                table: "Offers",
                column: "WorkModeId",
                principalTable: "WorkModes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OfferDtoTechnology_Technologies_TechnologiesId",
                table: "OfferDtoTechnology");

            migrationBuilder.DropForeignKey(
                name: "FK_Offers_Employers_EmployerId",
                table: "Offers");

            migrationBuilder.DropForeignKey(
                name: "FK_Offers_PositionLevels_PositionLevelId",
                table: "Offers");

            migrationBuilder.DropForeignKey(
                name: "FK_Offers_WorkModes_WorkModeId",
                table: "Offers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WorkModes",
                table: "WorkModes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Technologies",
                table: "Technologies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PositionLevels",
                table: "PositionLevels");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Employers",
                table: "Employers");

            migrationBuilder.DropColumn(
                name: "Requirements",
                table: "Offers");

            migrationBuilder.RenameTable(
                name: "WorkModes",
                newName: "WorkMode");

            migrationBuilder.RenameTable(
                name: "Technologies",
                newName: "Technology");

            migrationBuilder.RenameTable(
                name: "PositionLevels",
                newName: "PositionLevel");

            migrationBuilder.RenameTable(
                name: "Employers",
                newName: "Employer");

            migrationBuilder.RenameColumn(
                name: "Responsibilities",
                table: "Offers",
                newName: "Description");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WorkMode",
                table: "WorkMode",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Technology",
                table: "Technology",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PositionLevel",
                table: "PositionLevel",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Employer",
                table: "Employer",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OfferDtoTechnology_Technology_TechnologiesId",
                table: "OfferDtoTechnology",
                column: "TechnologiesId",
                principalTable: "Technology",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Offers_Employer_EmployerId",
                table: "Offers",
                column: "EmployerId",
                principalTable: "Employer",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Offers_PositionLevel_PositionLevelId",
                table: "Offers",
                column: "PositionLevelId",
                principalTable: "PositionLevel",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Offers_WorkMode_WorkModeId",
                table: "Offers",
                column: "WorkModeId",
                principalTable: "WorkMode",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
