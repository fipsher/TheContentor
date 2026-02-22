using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheContentor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropCriteriaFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContentAnalyses");

            migrationBuilder.DropTable(
                name: "AnalysisCriteria");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnalysisCriteria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Engine = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SystemPrompt = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisCriteria", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContentAnalyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CriteriaId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourcePostId = table.Column<Guid>(type: "uuid", nullable: false),
                    AiReasoning = table.Column<string>(type: "text", nullable: false),
                    AttractivenessScore = table.Column<float>(type: "real", nullable: false),
                    Labels = table.Column<string[]>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentAnalyses_AnalysisCriteria_CriteriaId",
                        column: x => x.CriteriaId,
                        principalTable: "AnalysisCriteria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContentAnalyses_SourcePosts_SourcePostId",
                        column: x => x.SourcePostId,
                        principalTable: "SourcePosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContentAnalyses_CriteriaId",
                table: "ContentAnalyses",
                column: "CriteriaId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentAnalyses_SourcePostId",
                table: "ContentAnalyses",
                column: "SourcePostId");
        }
    }
}
