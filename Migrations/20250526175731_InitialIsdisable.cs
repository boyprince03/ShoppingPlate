using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoppingPlate.Migrations
{
    /// <inheritdoc />
    public partial class InitialIsdisable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDisabled",
                table: "SellerApplications",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDisabled",
                table: "SellerApplications");
        }
    }
}
