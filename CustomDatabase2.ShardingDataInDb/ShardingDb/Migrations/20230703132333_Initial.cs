using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomDatabase2.ShardingDataInDb.ShardingDb.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShardingData",
                columns: table => new
                {
                    Name = table.Column<string>(type: "text", nullable: false),
                    DatabaseName = table.Column<string>(type: "text", nullable: true),
                    ConnectionName = table.Column<string>(type: "text", nullable: false),
                    DatabaseType = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShardingData", x => x.Name);
                });

            migrationBuilder.InsertData(
                table: "ShardingData",
                columns: new[] { "Name", "ConnectionName", "DatabaseName", "DatabaseType" },
                values: new object[] { "Default Database", "DefaultConnection", null, "PostgreSQL" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShardingData");
        }
    }
}
