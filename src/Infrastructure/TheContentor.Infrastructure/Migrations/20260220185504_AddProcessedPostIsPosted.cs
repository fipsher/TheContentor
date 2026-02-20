using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheContentor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessedPostIsPosted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPosted",
                table: "ProcessedPosts",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPosted",
                table: "ProcessedPosts");
        }
    }
}
