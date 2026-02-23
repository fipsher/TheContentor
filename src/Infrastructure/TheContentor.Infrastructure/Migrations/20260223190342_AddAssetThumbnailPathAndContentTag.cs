using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheContentor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetThumbnailPathAndContentTag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Assets");

            migrationBuilder.AddColumn<int>(
                name: "ContentTag",
                table: "Assets",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailPath",
                table: "Assets",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentTag",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "ThumbnailPath",
                table: "Assets");

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "Assets",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
