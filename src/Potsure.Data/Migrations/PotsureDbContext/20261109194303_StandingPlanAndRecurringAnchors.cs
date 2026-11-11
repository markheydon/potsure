using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Potsure.Data.Migrations;

/// <inheritdoc />
public partial class StandingPlanAndRecurringAnchors : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "ActualIncomeWindowMonths",
            table: "BudgetProfiles",
            type: "int",
            nullable: false,
            defaultValue: 3);

        migrationBuilder.AddColumn<decimal>(
            name: "EstimatedIncomeAmount",
            table: "BudgetProfiles",
            type: "decimal(18,2)",
            precision: 18,
            scale: 2,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "EstimatedIncomeCadence",
            table: "BudgetProfiles",
            type: "int",
            nullable: true);

        migrationBuilder.Sql(
            """
            UPDATE p
            SET
                p.EstimatedIncomeAmount = bp.BudgetedIncomeAmount,
                p.EstimatedIncomeCadence = CASE WHEN bp.BudgetedIncomeAmount > 0 THEN 2 ELSE NULL END
            FROM BudgetProfiles p
            INNER JOIN BudgetPeriods bp ON p.BudgetPeriodId = bp.Id;
            """);

        migrationBuilder.RenameColumn(
            name: "MonthlyEstimate",
            table: "RecurringExpenses",
            newName: "StoredMonthlyEstimate");

        migrationBuilder.AddColumn<int>(
            name: "AmountKind_New",
            table: "RecurringExpenses",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.Sql(
            """
            UPDATE RecurringExpenses
            SET AmountKind_New = CASE WHEN AmountKind = 'Variable' THEN 1 ELSE 0 END;
            """);

        migrationBuilder.DropColumn(
            name: "AmountKind",
            table: "RecurringExpenses");

        migrationBuilder.RenameColumn(
            name: "AmountKind_New",
            table: "RecurringExpenses",
            newName: "AmountKind");

        migrationBuilder.AddColumn<DateOnly>(
            name: "PaymentAnchorDate",
            table: "RecurringExpenses",
            type: "date",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "PaymentAnchorUserOverride",
            table: "RecurringExpenses",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.DropForeignKey(
            name: "FK_BudgetProfiles_BudgetPeriods_BudgetPeriodId",
            table: "BudgetProfiles");

        migrationBuilder.DropTable(
            name: "BudgetPeriods");

        migrationBuilder.DropIndex(
            name: "IX_BudgetProfiles_BudgetPeriodId",
            table: "BudgetProfiles");

        migrationBuilder.DropColumn(
            name: "BudgetPeriodId",
            table: "BudgetProfiles");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "PaymentAnchorDate",
            table: "RecurringExpenses");

        migrationBuilder.DropColumn(
            name: "PaymentAnchorUserOverride",
            table: "RecurringExpenses");

        migrationBuilder.DropColumn(
            name: "ActualIncomeWindowMonths",
            table: "BudgetProfiles");

        migrationBuilder.DropColumn(
            name: "EstimatedIncomeAmount",
            table: "BudgetProfiles");

        migrationBuilder.DropColumn(
            name: "EstimatedIncomeCadence",
            table: "BudgetProfiles");

        migrationBuilder.AddColumn<string>(
            name: "AmountKind_Old",
            table: "RecurringExpenses",
            type: "nvarchar(64)",
            maxLength: 64,
            nullable: false,
            defaultValue: "Fixed");

        migrationBuilder.Sql(
            """
            UPDATE RecurringExpenses
            SET AmountKind_Old = CASE WHEN AmountKind = 1 THEN 'Variable' ELSE 'Fixed' END;
            """);

        migrationBuilder.DropColumn(
            name: "AmountKind",
            table: "RecurringExpenses");

        migrationBuilder.RenameColumn(
            name: "AmountKind_Old",
            table: "RecurringExpenses",
            newName: "AmountKind");

        migrationBuilder.RenameColumn(
            name: "StoredMonthlyEstimate",
            table: "RecurringExpenses",
            newName: "MonthlyEstimate");

        migrationBuilder.AddColumn<Guid>(
            name: "BudgetPeriodId",
            table: "BudgetProfiles",
            type: "uniqueidentifier",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

        migrationBuilder.CreateTable(
            name: "BudgetPeriods",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                BudgetedIncomeAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                OwnerKey = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                StartDate = table.Column<DateOnly>(type: "date", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BudgetPeriods", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_BudgetProfiles_BudgetPeriodId",
            table: "BudgetProfiles",
            column: "BudgetPeriodId",
            unique: true);

        migrationBuilder.AddForeignKey(
            name: "FK_BudgetProfiles_BudgetPeriods_BudgetPeriodId",
            table: "BudgetProfiles",
            column: "BudgetPeriodId",
            principalTable: "BudgetPeriods",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }
}
