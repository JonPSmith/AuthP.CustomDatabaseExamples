using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomDatabase1.InvoiceCode.EfCoreCode.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "invoice");

            migrationBuilder.CreateTable(
                name: "Companies",
                schema: "invoice",
                columns: table => new
                {
                    CompanyTenantId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CompanyName = table.Column<string>(type: "TEXT", nullable: false),
                    DataKey = table.Column<string>(type: "TEXT", unicode: false, maxLength: 250, nullable: false),
                    AuthPTenantId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.CompanyTenantId);
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                schema: "invoice",
                columns: table => new
                {
                    InvoiceId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InvoiceName = table.Column<string>(type: "TEXT", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DataKey = table.Column<string>(type: "TEXT", unicode: false, maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.InvoiceId);
                });

            migrationBuilder.CreateTable(
                name: "LineItems",
                schema: "invoice",
                columns: table => new
                {
                    LineItemId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ItemName = table.Column<string>(type: "TEXT", nullable: false),
                    NumberItems = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "TEXT", precision: 9, scale: 2, nullable: false),
                    DataKey = table.Column<string>(type: "TEXT", unicode: false, maxLength: 250, nullable: false),
                    InvoiceId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineItems", x => x.LineItemId);
                    table.ForeignKey(
                        name: "FK_LineItems_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalSchema: "invoice",
                        principalTable: "Invoices",
                        principalColumn: "InvoiceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Companies_DataKey",
                schema: "invoice",
                table: "Companies",
                column: "DataKey");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_DataKey",
                schema: "invoice",
                table: "Invoices",
                column: "DataKey");

            migrationBuilder.CreateIndex(
                name: "IX_LineItems_DataKey",
                schema: "invoice",
                table: "LineItems",
                column: "DataKey");

            migrationBuilder.CreateIndex(
                name: "IX_LineItems_InvoiceId",
                schema: "invoice",
                table: "LineItems",
                column: "InvoiceId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Companies",
                schema: "invoice");

            migrationBuilder.DropTable(
                name: "LineItems",
                schema: "invoice");

            migrationBuilder.DropTable(
                name: "Invoices",
                schema: "invoice");
        }
    }
}
