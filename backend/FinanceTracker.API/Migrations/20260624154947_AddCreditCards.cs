using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceTracker.API.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditCards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CreditCards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastFourDigits = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreditLimit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AvailableCredit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RegularBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MsiBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentToAvoidInterest = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MinimumPayment = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CutoffDay = table.Column<int>(type: "int", nullable: false),
                    PaymentDueDay = table.Column<int>(type: "int", nullable: false),
                    LastStatementDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NextPaymentDueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditCards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MsiPromotions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreditCardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PurchaseDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OriginalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PendingBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MonthlyPayment = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalMonths = table.Column<int>(type: "int", nullable: false),
                    PaidMonths = table.Column<int>(type: "int", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MsiPromotions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MsiPromotions_CreditCards_CreditCardId",
                        column: x => x.CreditCardId,
                        principalTable: "CreditCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MsiPromotions_CreditCardId",
                table: "MsiPromotions",
                column: "CreditCardId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MsiPromotions");

            migrationBuilder.DropTable(
                name: "CreditCards");

        }
    }
}
