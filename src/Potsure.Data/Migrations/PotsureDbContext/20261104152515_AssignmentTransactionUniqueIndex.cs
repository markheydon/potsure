using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Potsure.Data.Migrations;

/// <inheritdoc />
public partial class AssignmentTransactionUniqueIndex : Migration
{
    private static readonly string[] AssignmentsOwnerTransactionIndexColumns = ["OwnerKey", "TransactionId"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "PaymentType",
            table: "RecurringExpenses",
            type: "nvarchar(64)",
            maxLength: 64,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(max)");

        migrationBuilder.AlterColumn<string>(
            name: "AmountKind",
            table: "RecurringExpenses",
            type: "nvarchar(64)",
            maxLength: 64,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(max)");

        migrationBuilder.CreateIndex(
            name: "IX_Transactions_CategoryId",
            table: "Transactions",
            column: "CategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_RecurringExpenses_CategoryId",
            table: "RecurringExpenses",
            column: "CategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_Assignments_CategoryId",
            table: "Assignments",
            column: "CategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_Assignments_OwnerKey_TransactionId",
            table: "Assignments",
            columns: AssignmentsOwnerTransactionIndexColumns,
            unique: true,
            filter: "[TransactionId] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_Assignments_RecurringExpenseId",
            table: "Assignments",
            column: "RecurringExpenseId");

        migrationBuilder.CreateIndex(
            name: "IX_Assignments_TransactionId",
            table: "Assignments",
            column: "TransactionId");

        migrationBuilder.AddForeignKey(
            name: "FK_Assignments_BudgetCategories_CategoryId",
            table: "Assignments",
            column: "CategoryId",
            principalTable: "BudgetCategories",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_Assignments_RecurringExpenses_RecurringExpenseId",
            table: "Assignments",
            column: "RecurringExpenseId",
            principalTable: "RecurringExpenses",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.AddForeignKey(
            name: "FK_Assignments_Transactions_TransactionId",
            table: "Assignments",
            column: "TransactionId",
            principalTable: "Transactions",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.AddForeignKey(
            name: "FK_RecurringExpenses_BudgetCategories_CategoryId",
            table: "RecurringExpenses",
            column: "CategoryId",
            principalTable: "BudgetCategories",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_Transactions_BudgetCategories_CategoryId",
            table: "Transactions",
            column: "CategoryId",
            principalTable: "BudgetCategories",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Assignments_BudgetCategories_CategoryId",
            table: "Assignments");

        migrationBuilder.DropForeignKey(
            name: "FK_Assignments_RecurringExpenses_RecurringExpenseId",
            table: "Assignments");

        migrationBuilder.DropForeignKey(
            name: "FK_Assignments_Transactions_TransactionId",
            table: "Assignments");

        migrationBuilder.DropForeignKey(
            name: "FK_RecurringExpenses_BudgetCategories_CategoryId",
            table: "RecurringExpenses");

        migrationBuilder.DropForeignKey(
            name: "FK_Transactions_BudgetCategories_CategoryId",
            table: "Transactions");

        migrationBuilder.DropIndex(
            name: "IX_Transactions_CategoryId",
            table: "Transactions");

        migrationBuilder.DropIndex(
            name: "IX_RecurringExpenses_CategoryId",
            table: "RecurringExpenses");

        migrationBuilder.DropIndex(
            name: "IX_Assignments_CategoryId",
            table: "Assignments");

        migrationBuilder.DropIndex(
            name: "IX_Assignments_OwnerKey_TransactionId",
            table: "Assignments");

        migrationBuilder.DropIndex(
            name: "IX_Assignments_RecurringExpenseId",
            table: "Assignments");

        migrationBuilder.DropIndex(
            name: "IX_Assignments_TransactionId",
            table: "Assignments");

        migrationBuilder.AlterColumn<string>(
            name: "PaymentType",
            table: "RecurringExpenses",
            type: "nvarchar(max)",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(64)",
            oldMaxLength: 64);

        migrationBuilder.AlterColumn<string>(
            name: "AmountKind",
            table: "RecurringExpenses",
            type: "nvarchar(max)",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(64)",
            oldMaxLength: 64);
    }
}
