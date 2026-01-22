using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheContentor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnalysisCriteria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SystemPrompt = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Engine = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisCriteria", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BackgroundAssets",
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
                    table.PrimaryKey("PK_BackgroundAssets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SourcePosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Platform = table.Column<int>(type: "integer", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RawText = table.Column<string>(type: "text", nullable: false),
                    MetadataJson = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourcePosts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContentAnalyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourcePostId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttractivenessScore = table.Column<float>(type: "real", nullable: false),
                    Labels = table.Column<string[]>(type: "text[]", nullable: false),
                    AiReasoning = table.Column<string>(type: "text", nullable: false),
                    CriteriaId = table.Column<Guid>(type: "uuid", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "SourceComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourcePostId = table.Column<Guid>(type: "uuid", nullable: false),
                    Author = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsIncluded = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourceComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SourceComments_SourcePosts_SourcePostId",
                        column: x => x.SourcePostId,
                        principalTable: "SourcePosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VideoProjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourcePostId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    BackgroundAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    TtsEngine = table.Column<int>(type: "integer", nullable: false),
                    VoiceProfileJson = table.Column<string>(type: "text", nullable: false),
                    SubtitleEngine = table.Column<int>(type: "integer", nullable: false),
                    SubStyle = table.Column<int>(type: "integer", nullable: false),
                    GeneratedAudioPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SubtitleFilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FinalVideoPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ErrorLog = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoProjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoProjects_BackgroundAssets_BackgroundAssetId",
                        column: x => x.BackgroundAssetId,
                        principalTable: "BackgroundAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VideoProjects_SourcePosts_SourcePostId",
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

            migrationBuilder.CreateIndex(
                name: "IX_SourceComments_SourcePostId",
                table: "SourceComments",
                column: "SourcePostId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoProjects_BackgroundAssetId",
                table: "VideoProjects",
                column: "BackgroundAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoProjects_SourcePostId",
                table: "VideoProjects",
                column: "SourcePostId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContentAnalyses");

            migrationBuilder.DropTable(
                name: "SourceComments");

            migrationBuilder.DropTable(
                name: "VideoProjects");

            migrationBuilder.DropTable(
                name: "AnalysisCriteria");

            migrationBuilder.DropTable(
                name: "BackgroundAssets");

            migrationBuilder.DropTable(
                name: "SourcePosts");
        }
    }
}
