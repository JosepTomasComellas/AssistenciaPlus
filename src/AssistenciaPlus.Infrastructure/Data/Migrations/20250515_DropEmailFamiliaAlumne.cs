using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace AssistenciaPlus.Infrastructure.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20250515100000_DropEmailFamiliaAlumne")]
public partial class DropEmailFamiliaAlumne : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "email_familia",
            table: "alumnes");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "email_familia",
            table: "alumnes",
            type: "character varying(120)",
            maxLength: 120,
            nullable: true);
    }
}
