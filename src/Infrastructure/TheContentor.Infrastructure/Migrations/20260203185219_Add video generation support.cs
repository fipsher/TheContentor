using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheContentor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Addvideogenerationsupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VideoContainer",
                table: "ProcessedPosts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoPath",
                table: "ProcessedPosts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoSettings",
                table: "ProcessedPosts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VideoStatus",
                table: "ProcessedPosts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SubtitleContainer",
                table: "ProcessedPostParts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubtitlePath",
                table: "ProcessedPostParts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoContainer",
                table: "ProcessedPostParts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoPath",
                table: "ProcessedPostParts",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VideoContainer",
                table: "ProcessedPosts");

            migrationBuilder.DropColumn(
                name: "VideoPath",
                table: "ProcessedPosts");

            migrationBuilder.DropColumn(
                name: "VideoSettings",
                table: "ProcessedPosts");

            migrationBuilder.DropColumn(
                name: "VideoStatus",
                table: "ProcessedPosts");

            migrationBuilder.DropColumn(
                name: "SubtitleContainer",
                table: "ProcessedPostParts");

            migrationBuilder.DropColumn(
                name: "SubtitlePath",
                table: "ProcessedPostParts");

            migrationBuilder.DropColumn(
                name: "VideoContainer",
                table: "ProcessedPostParts");

            migrationBuilder.DropColumn(
                name: "VideoPath",
                table: "ProcessedPostParts");
        }
    }
}
