using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSServer.Migrations
{
    public partial class AddSettledColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TotalSettledCredit",
                table: "CashDrawer",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalSettledCredit",
                table: "CashDrawer");
        }
    }
}
