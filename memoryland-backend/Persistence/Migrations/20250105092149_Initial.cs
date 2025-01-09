using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MemorylandTypes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PhotoAmount = table.Column<int>(type: "integer", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemorylandTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Memorylands",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MemorylandTypeId1 = table.Column<long>(type: "bigint", nullable: true),
                    MemorylandTypeId = table.Column<int>(type: "integer", nullable: false),
                    UserId1 = table.Column<long>(type: "bigint", nullable: true),
                    UserId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Memorylands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Memorylands_MemorylandTypes_MemorylandTypeId1",
                        column: x => x.MemorylandTypeId1,
                        principalTable: "MemorylandTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Memorylands_Users_UserId1",
                        column: x => x.UserId1,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PhotoAlbums",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UserId1 = table.Column<long>(type: "bigint", nullable: true),
                    UserId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhotoAlbums", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhotoAlbums_Users_UserId1",
                        column: x => x.UserId1,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MemorylandTokens",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Token = table.Column<Guid>(type: "uuid", nullable: false),
                    IsInternal = table.Column<bool>(type: "boolean", nullable: false),
                    MemorylandId1 = table.Column<long>(type: "bigint", nullable: true),
                    MemorylandId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemorylandTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemorylandTokens_Memorylands_MemorylandId1",
                        column: x => x.MemorylandId1,
                        principalTable: "Memorylands",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Photos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PhotoAlbumId1 = table.Column<long>(type: "bigint", nullable: true),
                    PhotoAlbumId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Photos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Photos_PhotoAlbums_PhotoAlbumId1",
                        column: x => x.PhotoAlbumId1,
                        principalTable: "PhotoAlbums",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MemorylandConfigurations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    MemorylandId1 = table.Column<long>(type: "bigint", nullable: true),
                    MemorylandId = table.Column<int>(type: "integer", nullable: false),
                    PhotoId1 = table.Column<long>(type: "bigint", nullable: true),
                    PhotoId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemorylandConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemorylandConfigurations_Memorylands_MemorylandId1",
                        column: x => x.MemorylandId1,
                        principalTable: "Memorylands",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MemorylandConfigurations_Photos_PhotoId1",
                        column: x => x.PhotoId1,
                        principalTable: "Photos",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_MemorylandConfigurations_MemorylandId1",
                table: "MemorylandConfigurations",
                column: "MemorylandId1");

            migrationBuilder.CreateIndex(
                name: "IX_MemorylandConfigurations_PhotoId1",
                table: "MemorylandConfigurations",
                column: "PhotoId1");

            migrationBuilder.CreateIndex(
                name: "IX_MemorylandConfigurations_Position_MemorylandId",
                table: "MemorylandConfigurations",
                columns: new[] { "Position", "MemorylandId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Memorylands_MemorylandTypeId1",
                table: "Memorylands",
                column: "MemorylandTypeId1");

            migrationBuilder.CreateIndex(
                name: "IX_Memorylands_Name",
                table: "Memorylands",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Memorylands_UserId1",
                table: "Memorylands",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_MemorylandTokens_IsInternal_MemorylandId",
                table: "MemorylandTokens",
                columns: new[] { "IsInternal", "MemorylandId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MemorylandTokens_MemorylandId1",
                table: "MemorylandTokens",
                column: "MemorylandId1");

            migrationBuilder.CreateIndex(
                name: "IX_MemorylandTokens_Token",
                table: "MemorylandTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MemorylandTypes_Name",
                table: "MemorylandTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhotoAlbums_Name",
                table: "PhotoAlbums",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhotoAlbums_UserId1",
                table: "PhotoAlbums",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_Name_PhotoAlbumId",
                table: "Photos",
                columns: new[] { "Name", "PhotoAlbumId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Photos_PhotoAlbumId1",
                table: "Photos",
                column: "PhotoAlbumId1");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MemorylandConfigurations");

            migrationBuilder.DropTable(
                name: "MemorylandTokens");

            migrationBuilder.DropTable(
                name: "Photos");

            migrationBuilder.DropTable(
                name: "Memorylands");

            migrationBuilder.DropTable(
                name: "PhotoAlbums");

            migrationBuilder.DropTable(
                name: "MemorylandTypes");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
