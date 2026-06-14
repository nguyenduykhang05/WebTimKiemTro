using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartRoomFinder.Migrations
{
    /// <inheritdoc />
    public partial class AddAIChatLogsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AIChatLogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SessionId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserMessage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BotReply = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExtractedJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIChatLogs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AIChatLogs");
        }
    }
}
