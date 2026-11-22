using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SevaSetu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ElderLoginAndPhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BlobKey",
                table: "KycDocuments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "KycDocuments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Elders",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CarePhotos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EpisodeId = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BlobKey = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    At = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarePhotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarePhotos_Episodes_EpisodeId",
                        column: x => x.EpisodeId,
                        principalTable: "Episodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Elders_UserId",
                table: "Elders",
                column: "UserId",
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CarePhotos_EpisodeId",
                table: "CarePhotos",
                column: "EpisodeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CarePhotos");

            migrationBuilder.DropIndex(
                name: "IX_Elders_UserId",
                table: "Elders");

            migrationBuilder.DropColumn(
                name: "BlobKey",
                table: "KycDocuments");

            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "KycDocuments");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Elders");
        }
    }
}
