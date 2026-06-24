using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartRoomFinder.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTransactionRoomIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceTransactions_Rooms_RoomId",
                table: "ServiceTransactions");

            migrationBuilder.AddColumn<int>(
                name: "CurrentPackage",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PackageExpiresAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RoomId",
                table: "ServiceTransactions",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceTransactions_Rooms_RoomId",
                table: "ServiceTransactions",
                column: "RoomId",
                principalTable: "Rooms",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceTransactions_Rooms_RoomId",
                table: "ServiceTransactions");

            migrationBuilder.DropColumn(
                name: "CurrentPackage",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PackageExpiresAt",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "RoomId",
                table: "ServiceTransactions",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceTransactions_Rooms_RoomId",
                table: "ServiceTransactions",
                column: "RoomId",
                principalTable: "Rooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
