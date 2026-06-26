using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiChild : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Children_UserId",
                table: "Children");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Children",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Every existing account has exactly one child (old unique index) - make it the active one.
            migrationBuilder.Sql("UPDATE \"Children\" SET \"IsActive\" = true;");

            migrationBuilder.CreateIndex(
                name: "IX_Children_UserId_IsActive",
                table: "Children",
                columns: new[] { "UserId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Children_UserId_IsActive",
                table: "Children");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Children");

            migrationBuilder.CreateIndex(
                name: "IX_Children_UserId",
                table: "Children",
                column: "UserId",
                unique: true);
        }
    }
}
