using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheContentor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Updatepostentities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SourceComments_SourcePostId",
                table: "SourceComments");

            migrationBuilder.DropColumn(
                name: "Author",
                table: "SourceComments");

            migrationBuilder.RenameColumn(
                name: "Order",
                table: "SourceComments",
                newName: "Score");

            migrationBuilder.RenameColumn(
                name: "IsIncluded",
                table: "SourceComments",
                newName: "IsDeleted");

            migrationBuilder.RenameColumn(
                name: "Body",
                table: "SourceComments",
                newName: "RawText");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "SourcePosts",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "SourcePosts",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Platform",
                table: "SourcePosts",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "MetadataJson",
                table: "SourcePosts",
                type: "text",
                nullable: false,
                defaultValue: "{}",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ExternalId",
                table: "SourcePosts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "AuthorExternalId",
                table: "SourcePosts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AuthorName",
                table: "SourcePosts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "CommentCount",
                table: "SourcePosts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Community",
                table: "SourcePosts",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CommunityExternalId",
                table: "SourcePosts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentHash",
                table: "SourcePosts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedUtc",
                table: "SourcePosts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "ExternalUrl",
                table: "SourcePosts",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Flairs",
                table: "SourcePosts",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "IngestedUtc",
                table: "SourcePosts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<bool>(
                name: "IsNsfw",
                table: "SourcePosts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSpoiler",
                table: "SourcePosts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "SourcePosts",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastRefreshedUtc",
                table: "SourcePosts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RawHtml",
                table: "SourcePosts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Score",
                table: "SourcePosts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "StatusReason",
                table: "SourcePosts",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "UpvoteRatio",
                table: "SourcePosts",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WordCount",
                table: "SourcePosts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AuthorName",
                table: "SourceComments",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedUtc",
                table: "SourceComments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "SourceComments",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MetadataJson",
                table: "SourceComments",
                type: "text",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<string>(
                name: "ParentExternalId",
                table: "SourceComments",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "PostMetricSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourcePostId = table.Column<Guid>(type: "uuid", nullable: false),
                    CapturedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    CommentCount = table.Column<int>(type: "integer", nullable: false),
                    UpvoteRatio = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostMetricSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostMetricSnapshots_SourcePosts_SourcePostId",
                        column: x => x.SourcePostId,
                        principalTable: "SourcePosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RedditPostData",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Subreddit = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Permalink = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    FullName = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsSelfPost = table.Column<bool>(type: "boolean", nullable: false),
                    LinkUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    Domain = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    FlairText = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsAuthorDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    AuthorCreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AuthorLinkKarma = table.Column<int>(type: "integer", nullable: true),
                    AuthorCommentKarma = table.Column<int>(type: "integer", nullable: true),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false),
                    IsRemoved = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    IsStickied = table.Column<bool>(type: "boolean", nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    TotalAwardsReceived = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RedditPostData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RedditPostData_SourcePosts_Id",
                        column: x => x.Id,
                        principalTable: "SourcePosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SourcePosts_ContentHash",
                table: "SourcePosts",
                column: "ContentHash");

            migrationBuilder.CreateIndex(
                name: "IX_SourcePosts_IngestedUtc",
                table: "SourcePosts",
                column: "IngestedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SourcePosts_Platform_Community_CreatedUtc",
                table: "SourcePosts",
                columns: new[] { "Platform", "Community", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SourcePosts_Platform_Community_Score_CreatedUtc",
                table: "SourcePosts",
                columns: new[] { "Platform", "Community", "Score", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SourcePosts_Platform_ExternalId",
                table: "SourcePosts",
                columns: new[] { "Platform", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SourcePosts_Status",
                table: "SourcePosts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SourceComments_CreatedUtc",
                table: "SourceComments",
                column: "CreatedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SourceComments_SourcePostId_ExternalId",
                table: "SourceComments",
                columns: new[] { "SourcePostId", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SourceComments_SourcePostId_Score",
                table: "SourceComments",
                columns: new[] { "SourcePostId", "Score" });

            migrationBuilder.CreateIndex(
                name: "IX_PostMetricSnapshots_SourcePostId_CapturedUtc",
                table: "PostMetricSnapshots",
                columns: new[] { "SourcePostId", "CapturedUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RedditPostData_FullName",
                table: "RedditPostData",
                column: "FullName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RedditPostData_Subreddit",
                table: "RedditPostData",
                column: "Subreddit");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PostMetricSnapshots");

            migrationBuilder.DropTable(
                name: "RedditPostData");

            migrationBuilder.DropIndex(
                name: "IX_SourcePosts_ContentHash",
                table: "SourcePosts");

            migrationBuilder.DropIndex(
                name: "IX_SourcePosts_IngestedUtc",
                table: "SourcePosts");

            migrationBuilder.DropIndex(
                name: "IX_SourcePosts_Platform_Community_CreatedUtc",
                table: "SourcePosts");

            migrationBuilder.DropIndex(
                name: "IX_SourcePosts_Platform_Community_Score_CreatedUtc",
                table: "SourcePosts");

            migrationBuilder.DropIndex(
                name: "IX_SourcePosts_Platform_ExternalId",
                table: "SourcePosts");

            migrationBuilder.DropIndex(
                name: "IX_SourcePosts_Status",
                table: "SourcePosts");

            migrationBuilder.DropIndex(
                name: "IX_SourceComments_CreatedUtc",
                table: "SourceComments");

            migrationBuilder.DropIndex(
                name: "IX_SourceComments_SourcePostId_ExternalId",
                table: "SourceComments");

            migrationBuilder.DropIndex(
                name: "IX_SourceComments_SourcePostId_Score",
                table: "SourceComments");

            migrationBuilder.DropColumn(
                name: "AuthorExternalId",
                table: "SourcePosts");

            migrationBuilder.DropColumn(
                name: "AuthorName",
                table: "SourcePosts");

            migrationBuilder.DropColumn(
                name: "CommentCount",
                table: "SourcePosts");

            migrationBuilder.DropColumn(
                name: "Community",
                table: "SourcePosts");

            migrationBuilder.DropColumn(
                name: "CommunityExternalId",
                table: "SourcePosts");

            migrationBuilder.DropColumn(
                name: "ContentHash",
                table: "SourcePosts");

            migrationBuilder.DropColumn(
                name: "CreatedUtc",
                table: "SourcePosts");

            migrationBuilder.DropColumn(
                name: "ExternalUrl",
                table: "SourcePosts");

            migrationBuilder.DropColumn(
                name: "Flairs",
                table: "SourcePosts");

            migrationBuilder.DropColumn(
                name: "IngestedUtc",
                table: "SourcePosts");

            migrationBuilder.DropColumn(
                name: "IsNsfw",
                table: "SourcePosts");

            migrationBuilder.DropColumn(
                name: "IsSpoiler",
                table: "SourcePosts");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "SourcePosts");

            migrationBuilder.DropColumn(
                name: "LastRefreshedUtc",
                table: "SourcePosts");

            migrationBuilder.DropColumn(
                name: "RawHtml",
                table: "SourcePosts");

            migrationBuilder.DropColumn(
                name: "Score",
                table: "SourcePosts");

            migrationBuilder.DropColumn(
                name: "StatusReason",
                table: "SourcePosts");

            migrationBuilder.DropColumn(
                name: "UpvoteRatio",
                table: "SourcePosts");

            migrationBuilder.DropColumn(
                name: "WordCount",
                table: "SourcePosts");

            migrationBuilder.DropColumn(
                name: "AuthorName",
                table: "SourceComments");

            migrationBuilder.DropColumn(
                name: "CreatedUtc",
                table: "SourceComments");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "SourceComments");

            migrationBuilder.DropColumn(
                name: "MetadataJson",
                table: "SourceComments");

            migrationBuilder.DropColumn(
                name: "ParentExternalId",
                table: "SourceComments");

            migrationBuilder.RenameColumn(
                name: "Score",
                table: "SourceComments",
                newName: "Order");

            migrationBuilder.RenameColumn(
                name: "RawText",
                table: "SourceComments",
                newName: "Body");

            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "SourceComments",
                newName: "IsIncluded");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "SourcePosts",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "SourcePosts",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<int>(
                name: "Platform",
                table: "SourcePosts",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<string>(
                name: "MetadataJson",
                table: "SourcePosts",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "{}");

            migrationBuilder.AlterColumn<string>(
                name: "ExternalId",
                table: "SourcePosts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AddColumn<string>(
                name: "Author",
                table: "SourceComments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_SourceComments_SourcePostId",
                table: "SourceComments",
                column: "SourcePostId");
        }
    }
}
