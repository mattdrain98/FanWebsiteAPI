using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FanWebsiteAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddChatParticipantLastReadAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastReadAt",
                table: "ChatParticipants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ForumId_CreatedAt",
                table: "ChatMessages",
                columns: new[] { "ForumId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChatMessages_ForumId_CreatedAt",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "LastReadAt",
                table: "ChatParticipants");
        }
    }
}
