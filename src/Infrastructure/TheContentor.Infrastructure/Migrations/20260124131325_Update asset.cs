using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheContentor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Updateasset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM \"Assets\"");
            
            migrationBuilder.DropColumn(
                name: "BlobPath",
                table: "Assets");

            migrationBuilder.RenameColumn(
                name: "FileName",
                table: "Assets",
                newName: "Name");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "Duration",
                table: "Assets",
                type: "interval",
                nullable: true,
                oldClrType: typeof(TimeSpan),
                oldType: "interval");

            migrationBuilder.AddColumn<string>(
                name: "BlobPath_AssetPath",
                table: "Assets",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BlobPath_ContainerName",
                table: "Assets",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_Name",
                table: "Assets",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Assets_Name",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "BlobPath_AssetPath",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "BlobPath_ContainerName",
                table: "Assets");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Assets",
                newName: "FileName");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "Duration",
                table: "Assets",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0),
                oldClrType: typeof(TimeSpan),
                oldType: "interval",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BlobPath",
                table: "Assets",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }
    }
}
