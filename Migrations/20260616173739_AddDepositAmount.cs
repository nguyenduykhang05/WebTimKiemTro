using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartRoomFinder.Migrations
{
    /// <inheritdoc />
    public partial class AddDepositAmount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "DepositAmount",
                table: "Rooms",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DepositAmount",
                table: "Rooms");
        }
    }
}
