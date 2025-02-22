using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Remove_Token_IsInternal_Column : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MemorylandTokens_IsInternal_MemorylandId",
                table: "MemorylandTokens");

            migrationBuilder.DropIndex(
                name: "IX_MemorylandTokens_MemorylandId",
                table: "MemorylandTokens");

            migrationBuilder.DropColumn(
                name: "IsInternal",
                table: "MemorylandTokens");

            migrationBuilder.CreateIndex(
                name: "IX_MemorylandTokens_MemorylandId",
                table: "MemorylandTokens",
                column: "MemorylandId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MemorylandTokens_MemorylandId",
                table: "MemorylandTokens");

            migrationBuilder.AddColumn<bool>(
                name: "IsInternal",
                table: "MemorylandTokens",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_MemorylandTokens_IsInternal_MemorylandId",
                table: "MemorylandTokens",
                columns: new[] { "IsInternal", "MemorylandId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MemorylandTokens_MemorylandId",
                table: "MemorylandTokens",
                column: "MemorylandId");
        }
    }
}
