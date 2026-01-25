using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheContentor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Addtitledescriptionandpart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProcessedPostParts_ProcessedPostId",
                table: "ProcessedPostParts");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "ProcessedPosts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "ProcessedPosts",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Part",
                table: "ProcessedPostParts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedPostParts_ProcessedPostId_Part",
                table: "ProcessedPostParts",
                columns: new[] { "ProcessedPostId", "Part" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProcessedPostParts_ProcessedPostId_Part",
                table: "ProcessedPostParts");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "ProcessedPosts");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "ProcessedPosts");

            migrationBuilder.DropColumn(
                name: "Part",
                table: "ProcessedPostParts");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedPostParts_ProcessedPostId",
                table: "ProcessedPostParts",
                column: "ProcessedPostId");
        }
    }
}
