using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStorySeries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Pre-launch only: existing chapters have no series. The new SeriesId is required, so
            // clear the (test) chapters; users simply regenerate. Generation logs are kept.
            migrationBuilder.Sql("DELETE FROM \"StoryChapters\";");

            migrationBuilder.DropIndex(
                name: "IX_StoryChapters_ChildId_Number",
                table: "StoryChapters");

            migrationBuilder.AddColumn<long>(
                name: "SeriesId",
                table: "StoryChapters",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "StorySeries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChildId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ArchivedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorySeries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StorySeries_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StoryChapters_ChildId",
                table: "StoryChapters",
                column: "ChildId");

            migrationBuilder.CreateIndex(
                name: "IX_StoryChapters_SeriesId_Number",
                table: "StoryChapters",
                columns: new[] { "SeriesId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StorySeries_ChildId_IsActive",
                table: "StorySeries",
                columns: new[] { "ChildId", "IsActive" });

            migrationBuilder.AddForeignKey(
                name: "FK_StoryChapters_StorySeries_SeriesId",
                table: "StoryChapters",
                column: "SeriesId",
                principalTable: "StorySeries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StoryChapters_StorySeries_SeriesId",
                table: "StoryChapters");

            migrationBuilder.DropTable(
                name: "StorySeries");

            migrationBuilder.DropIndex(
                name: "IX_StoryChapters_ChildId",
                table: "StoryChapters");

            migrationBuilder.DropIndex(
                name: "IX_StoryChapters_SeriesId_Number",
                table: "StoryChapters");

            migrationBuilder.DropColumn(
                name: "SeriesId",
                table: "StoryChapters");

            migrationBuilder.CreateIndex(
                name: "IX_StoryChapters_ChildId_Number",
                table: "StoryChapters",
                columns: new[] { "ChildId", "Number" },
                unique: true);
        }
    }
}
