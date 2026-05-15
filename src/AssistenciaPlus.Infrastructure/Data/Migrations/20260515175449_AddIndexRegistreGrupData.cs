using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssistenciaPlus.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexRegistreGrupData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_registres_assistencia_grup_id_data",
                table: "registres_assistencia",
                columns: new[] { "grup_id", "data" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_registres_assistencia_grup_id_data",
                table: "registres_assistencia");
        }
    }
}
