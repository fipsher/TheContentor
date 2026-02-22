using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheContentor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduledPost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScheduledPosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduledDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SourcePostId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledPosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduledPosts_SourcePosts_SourcePostId",
                        column: x => x.SourcePostId,
                        principalTable: "SourcePosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledPosts_ScheduledDate",
                table: "ScheduledPosts",
                column: "ScheduledDate");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledPosts_SourcePostId",
                table: "ScheduledPosts",
                column: "SourcePostId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScheduledPosts");
        }
    }
}
