using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FanWebsiteAPI.Migrations
{
    /// <inheritdoc />
    public partial class SyncPostImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreatedOn",
                table: "PostImages",
                newName: "UpdatedOn");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UpdatedOn",
                table: "PostImages",
                newName: "CreatedOn");
        }
    }
}
