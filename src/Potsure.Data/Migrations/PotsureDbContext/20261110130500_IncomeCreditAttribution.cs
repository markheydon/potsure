using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Potsure.Data.Migrations;

/// <inheritdoc />
public partial class IncomeCreditAttribution : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsIncomeAttribution",
            table: "Assignments",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<Guid>(
            name: "IncomeSourceId",
            table: "Assignments",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "CategoryId",
            table: "Assignments",
            type: "uniqueidentifier",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "uniqueidentifier");

        migrationBuilder.CreateIndex(
            name: "IX_Assignments_IncomeSourceId",
            table: "Assignments",
            column: "IncomeSourceId");

        migrationBuilder.AddForeignKey(
            name: "FK_Assignments_IncomeSources_IncomeSourceId",
            table: "Assignments",
            column: "IncomeSourceId",
            principalTable: "IncomeSources",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Assignments_IncomeSources_IncomeSourceId",
            table: "Assignments");

        migrationBuilder.DropIndex(
            name: "IX_Assignments_IncomeSourceId",
            table: "Assignments");

        migrationBuilder.DropColumn(
            name: "IsIncomeAttribution",
            table: "Assignments");

        migrationBuilder.DropColumn(
            name: "IncomeSourceId",
            table: "Assignments");

        migrationBuilder.AlterColumn<Guid>(
            name: "CategoryId",
            table: "Assignments",
            type: "uniqueidentifier",
            nullable: false,
            defaultValue: Guid.Empty,
            oldClrType: typeof(Guid),
            oldType: "uniqueidentifier",
            oldNullable: true);
    }
}
