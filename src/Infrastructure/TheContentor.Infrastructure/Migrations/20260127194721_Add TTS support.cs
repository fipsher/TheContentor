using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheContentor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTTSsupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DescriptionAudioContainer",
                table: "ProcessedPosts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionAudioPath",
                table: "ProcessedPosts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TtsSettings",
                table: "ProcessedPosts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TtsStatus",
                table: "ProcessedPosts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AudioContainer",
                table: "ProcessedPostParts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AudioPath",
                table: "ProcessedPostParts",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DescriptionAudioContainer",
                table: "ProcessedPosts");

            migrationBuilder.DropColumn(
                name: "DescriptionAudioPath",
                table: "ProcessedPosts");

            migrationBuilder.DropColumn(
                name: "TtsSettings",
                table: "ProcessedPosts");

            migrationBuilder.DropColumn(
                name: "TtsStatus",
                table: "ProcessedPosts");

            migrationBuilder.DropColumn(
                name: "AudioContainer",
                table: "ProcessedPostParts");

            migrationBuilder.DropColumn(
                name: "AudioPath",
                table: "ProcessedPostParts");
        }
    }
}
