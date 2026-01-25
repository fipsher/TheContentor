using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheContentor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Addprocessedpostentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProcessedPosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Hashtags = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedPosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessedPosts_SourcePosts_Id",
                        column: x => x.Id,
                        principalTable: "SourcePosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProcessedPostParts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessedPostId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessedText = table.Column<string>(type: "text", nullable: false),
                    Hashtags = table.Column<List<string>>(type: "text[]", nullable: false),
                    PublishedTo = table.Column<int[]>(type: "integer[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedPostParts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessedPostParts_ProcessedPosts_ProcessedPostId",
                        column: x => x.ProcessedPostId,
                        principalTable: "ProcessedPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedPostParts_ProcessedPostId",
                table: "ProcessedPostParts",
                column: "ProcessedPostId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcessedPostParts");

            migrationBuilder.DropTable(
                name: "ProcessedPosts");
        }
    }
}
