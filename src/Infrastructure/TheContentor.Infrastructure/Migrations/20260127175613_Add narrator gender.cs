using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheContentor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Addnarratorgender : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NarratorGender",
                table: "ProcessedPosts",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NarratorGender",
                table: "ProcessedPosts");
        }
    }
}
