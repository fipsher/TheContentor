using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheContentor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Renameassets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VideoProjects_BackgroundAssets_BackgroundAssetId",
                table: "VideoProjects");

            migrationBuilder.DropTable(
                name: "BackgroundAssets");

            migrationBuilder.RenameColumn(
                name: "BackgroundAssetId",
                table: "VideoProjects",
                newName: "AssetId");

            migrationBuilder.RenameIndex(
                name: "IX_VideoProjects_BackgroundAssetId",
                table: "VideoProjects",
                newName: "IX_VideoProjects_AssetId");

            migrationBuilder.CreateTable(
                name: "Assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    LocalPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Tags = table.Column<string>(type: "text", nullable: false),
                    Duration = table.Column<TimeSpan>(type: "interval", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assets", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_VideoProjects_Assets_AssetId",
                table: "VideoProjects",
                column: "AssetId",
                principalTable: "Assets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VideoProjects_Assets_AssetId",
                table: "VideoProjects");

            migrationBuilder.DropTable(
                name: "Assets");

            migrationBuilder.RenameColumn(
                name: "AssetId",
                table: "VideoProjects",
                newName: "BackgroundAssetId");

            migrationBuilder.RenameIndex(
                name: "IX_VideoProjects_AssetId",
                table: "VideoProjects",
                newName: "IX_VideoProjects_BackgroundAssetId");

            migrationBuilder.CreateTable(
                name: "BackgroundAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Duration = table.Column<TimeSpan>(type: "interval", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LocalPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Tags = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackgroundAssets", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_VideoProjects_BackgroundAssets_BackgroundAssetId",
                table: "VideoProjects",
                column: "BackgroundAssetId",
                principalTable: "BackgroundAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
