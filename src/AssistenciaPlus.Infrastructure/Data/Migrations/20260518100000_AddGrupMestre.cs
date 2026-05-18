using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssistenciaPlus.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGrupMestre : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "grups_mestres",
                columns: table => new
                {
                    grup_id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuari_id = table.Column<Guid>(type: "uuid", nullable: false),
                    afegit_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_grups_mestres", x => new { x.grup_id, x.usuari_id });
                    table.ForeignKey(
                        name: "fk_grups_mestres_grups_grup_id",
                        column: x => x.grup_id,
                        principalTable: "grups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_grups_mestres_usuaris_usuari_id",
                        column: x => x.usuari_id,
                        principalTable: "usuaris",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_grups_mestres_usuari_id",
                table: "grups_mestres",
                column: "usuari_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "grups_mestres");
        }
    }
}
