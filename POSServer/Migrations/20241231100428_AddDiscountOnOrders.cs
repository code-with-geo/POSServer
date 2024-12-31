using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSServer.Migrations
{
    public partial class AddDiscountOnOrders : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DiscountId",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DiscountId",
                table: "OrderProducts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DiscountsDiscountId",
                table: "OrderProducts",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_DiscountId",
                table: "Orders",
                column: "DiscountId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderProducts_DiscountsDiscountId",
                table: "OrderProducts",
                column: "DiscountsDiscountId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderProducts_Discounts_DiscountsDiscountId",
                table: "OrderProducts",
                column: "DiscountsDiscountId",
                principalTable: "Discounts",
                principalColumn: "DiscountId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Discounts_DiscountId",
                table: "Orders",
                column: "DiscountId",
                principalTable: "Discounts",
                principalColumn: "DiscountId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderProducts_Discounts_DiscountsDiscountId",
                table: "OrderProducts");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Discounts_DiscountId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_DiscountId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_OrderProducts_DiscountsDiscountId",
                table: "OrderProducts");

            migrationBuilder.DropColumn(
                name: "DiscountId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DiscountId",
                table: "OrderProducts");

            migrationBuilder.DropColumn(
                name: "DiscountsDiscountId",
                table: "OrderProducts");
        }
    }
}
