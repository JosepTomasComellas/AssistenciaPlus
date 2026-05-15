using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using AssistenciaPlus.Infrastructure.Data;

#nullable disable

namespace AssistenciaPlus.Infrastructure.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20250515000000_InitialCreate")]
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ── anys_academics ────────────────────────────────
        migrationBuilder.CreateTable(
            name: "anys_academics",
            columns: table => new
            {
                id             = table.Column<Guid>(nullable: false),
                nom            = table.Column<string>(maxLength: 20, nullable: false),
                data_inici     = table.Column<DateOnly>(nullable: false),
                data_fi        = table.Column<DateOnly>(nullable: false),
                es_actiu       = table.Column<bool>(nullable: false, defaultValue: false),
                inici_t1       = table.Column<DateOnly>(nullable: false),
                fi_t1          = table.Column<DateOnly>(nullable: false),
                inici_t2       = table.Column<DateOnly>(nullable: false),
                fi_t2          = table.Column<DateOnly>(nullable: false),
                inici_t3       = table.Column<DateOnly>(nullable: false),
                fi_t3          = table.Column<DateOnly>(nullable: false),
                created_at     = table.Column<DateTime>(nullable: false),
                updated_at     = table.Column<DateTime>(nullable: true),
                is_deleted     = table.Column<bool>(nullable: false, defaultValue: false)
            },
            constraints: table => table.PrimaryKey("pk_anys_academics", x => x.id));

        // ── dies_calendari ────────────────────────────────
        migrationBuilder.CreateTable(
            name: "dies_calendari",
            columns: table => new
            {
                id               = table.Column<Guid>(nullable: false),
                any_academic_id  = table.Column<Guid>(nullable: false),
                data             = table.Column<DateOnly>(nullable: false),
                tipus_dia        = table.Column<int>(nullable: false, defaultValue: 0),
                descripcio       = table.Column<string>(maxLength: 120, nullable: true),
                created_at       = table.Column<DateTime>(nullable: false),
                updated_at       = table.Column<DateTime>(nullable: true),
                is_deleted       = table.Column<bool>(nullable: false, defaultValue: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_dies_calendari", x => x.id);
                table.ForeignKey("fk_dies_calendari_anys_academics",
                    x => x.any_academic_id, "anys_academics", "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex("ix_dies_calendari_any_data",
            "dies_calendari", new[] { "any_academic_id", "data" }, unique: true);

        // ── cicles ────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "cicles",
            columns: table => new
            {
                id         = table.Column<Guid>(nullable: false),
                nom        = table.Column<string>(maxLength: 60, nullable: false),
                ordre      = table.Column<int>(nullable: false),
                created_at = table.Column<DateTime>(nullable: false),
                updated_at = table.Column<DateTime>(nullable: true),
                is_deleted = table.Column<bool>(nullable: false, defaultValue: false)
            },
            constraints: table => table.PrimaryKey("pk_cicles", x => x.id));

        // ── cursos ────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "cursos",
            columns: table => new
            {
                id               = table.Column<Guid>(nullable: false),
                cicle_id         = table.Column<Guid>(nullable: false),
                nom              = table.Column<string>(maxLength: 60, nullable: false),
                codi             = table.Column<string>(maxLength: 10, nullable: false),
                ordre            = table.Column<int>(nullable: false),
                usa_mode_fusteta = table.Column<bool>(nullable: false, defaultValue: false),
                created_at       = table.Column<DateTime>(nullable: false),
                updated_at       = table.Column<DateTime>(nullable: true),
                is_deleted       = table.Column<bool>(nullable: false, defaultValue: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_cursos", x => x.id);
                table.ForeignKey("fk_cursos_cicles",
                    x => x.cicle_id, "cicles", "id",
                    onDelete: ReferentialAction.Restrict);
            });

        // ── usuaris ───────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "usuaris",
            columns: table => new
            {
                id            = table.Column<Guid>(nullable: false),
                nom           = table.Column<string>(maxLength: 60, nullable: false),
                cognom1       = table.Column<string>(maxLength: 60, nullable: false),
                cognom2       = table.Column<string>(maxLength: 60, nullable: true),
                email         = table.Column<string>(maxLength: 120, nullable: false),
                password_hash = table.Column<string>(maxLength: 256, nullable: false),
                rol           = table.Column<int>(nullable: false, defaultValue: 0),
                foto_path     = table.Column<string>(nullable: true),
                es_actiu      = table.Column<bool>(nullable: false, defaultValue: true),
                idioma        = table.Column<string>(maxLength: 2, nullable: false, defaultValue: "ca"),
                ultim_acces   = table.Column<DateTime>(nullable: true),
                created_at    = table.Column<DateTime>(nullable: false),
                updated_at    = table.Column<DateTime>(nullable: true),
                is_deleted    = table.Column<bool>(nullable: false, defaultValue: false)
            },
            constraints: table => table.PrimaryKey("pk_usuaris", x => x.id));

        migrationBuilder.CreateIndex("ix_usuaris_email", "usuaris", "email", unique: true);

        // ── grups ─────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "grups",
            columns: table => new
            {
                id              = table.Column<Guid>(nullable: false),
                curs_id         = table.Column<Guid>(nullable: false),
                any_academic_id = table.Column<Guid>(nullable: false),
                lletra          = table.Column<string>(maxLength: 2, nullable: false, defaultValue: ""),
                tutor_id        = table.Column<Guid>(nullable: true),
                created_at      = table.Column<DateTime>(nullable: false),
                updated_at      = table.Column<DateTime>(nullable: true),
                is_deleted      = table.Column<bool>(nullable: false, defaultValue: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_grups", x => x.id);
                table.ForeignKey("fk_grups_cursos",
                    x => x.curs_id, "cursos", "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("fk_grups_anys_academics",
                    x => x.any_academic_id, "anys_academics", "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("fk_grups_usuaris_tutor",
                    x => x.tutor_id, "usuaris", "id", onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex("ix_grups_curs_any_lletra",
            "grups", new[] { "curs_id", "any_academic_id", "lletra" }, unique: true);

        // ── alumnes ───────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "alumnes",
            columns: table => new
            {
                id              = table.Column<Guid>(nullable: false),
                nom             = table.Column<string>(maxLength: 60, nullable: false),
                cognom1         = table.Column<string>(maxLength: 60, nullable: false),
                cognom2         = table.Column<string>(maxLength: 60, nullable: true),
                data_naixement  = table.Column<DateOnly>(nullable: true),
                foto_path       = table.Column<string>(nullable: true),
                email_familia   = table.Column<string>(maxLength: 120, nullable: true),
                grup_id         = table.Column<Guid>(nullable: false),
                ordre_fusteta   = table.Column<int>(nullable: false, defaultValue: 0),
                es_actiu        = table.Column<bool>(nullable: false, defaultValue: true),
                created_at      = table.Column<DateTime>(nullable: false),
                updated_at      = table.Column<DateTime>(nullable: true),
                is_deleted      = table.Column<bool>(nullable: false, defaultValue: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_alumnes", x => x.id);
                table.ForeignKey("fk_alumnes_grups",
                    x => x.grup_id, "grups", "id", onDelete: ReferentialAction.Restrict);
            });

        // ── franjes_horaries ──────────────────────────────
        migrationBuilder.CreateTable(
            name: "franjes_horaries",
            columns: table => new
            {
                id                    = table.Column<Guid>(nullable: false),
                nom                   = table.Column<string>(maxLength: 40, nullable: false),
                hora_inici            = table.Column<TimeOnly>(nullable: false),
                hora_fi               = table.Column<TimeOnly>(nullable: false),
                es_mati               = table.Column<bool>(nullable: false),
                es_jornada_intensiva  = table.Column<bool>(nullable: false),
                ordre                 = table.Column<int>(nullable: false),
                created_at            = table.Column<DateTime>(nullable: false),
                updated_at            = table.Column<DateTime>(nullable: true),
                is_deleted            = table.Column<bool>(nullable: false, defaultValue: false)
            },
            constraints: table => table.PrimaryKey("pk_franjes_horaries", x => x.id));

        // ── registres_assistencia ─────────────────────────
        migrationBuilder.CreateTable(
            name: "registres_assistencia",
            columns: table => new
            {
                id                = table.Column<Guid>(nullable: false),
                grup_id           = table.Column<Guid>(nullable: false),
                franja_horaria_id = table.Column<Guid>(nullable: false),
                data              = table.Column<DateOnly>(nullable: false),
                mestre_id         = table.Column<Guid>(nullable: false),
                degat_at          = table.Column<DateTime>(nullable: false),
                observacio        = table.Column<string>(maxLength: 500, nullable: true),
                created_at        = table.Column<DateTime>(nullable: false),
                updated_at        = table.Column<DateTime>(nullable: true),
                is_deleted        = table.Column<bool>(nullable: false, defaultValue: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_registres_assistencia", x => x.id);
                table.ForeignKey("fk_registres_grups",
                    x => x.grup_id, "grups", "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("fk_registres_franjes",
                    x => x.franja_horaria_id, "franjes_horaries", "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("fk_registres_mestres",
                    x => x.mestre_id, "usuaris", "id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex("ix_registres_grup_franja_data",
            "registres_assistencia", new[] { "grup_id", "franja_horaria_id", "data" }, unique: true);

        // ── assistencies_alumnes ──────────────────────────
        migrationBuilder.CreateTable(
            name: "assistencies_alumnes",
            columns: table => new
            {
                id                       = table.Column<Guid>(nullable: false),
                registre_assistencia_id  = table.Column<Guid>(nullable: false),
                alumne_id                = table.Column<Guid>(nullable: false),
                estat                    = table.Column<int>(nullable: false, defaultValue: 0),
                motiu                    = table.Column<int>(nullable: true),
                torna                    = table.Column<bool>(nullable: true),
                minuts_retard            = table.Column<int>(nullable: true),
                observacio               = table.Column<string>(maxLength: 500, nullable: true),
                aplicat_resta_dia        = table.Column<bool>(nullable: false, defaultValue: false),
                created_at               = table.Column<DateTime>(nullable: false),
                updated_at               = table.Column<DateTime>(nullable: true),
                is_deleted               = table.Column<bool>(nullable: false, defaultValue: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_assistencies_alumnes", x => x.id);
                table.ForeignKey("fk_assistencies_registres",
                    x => x.registre_assistencia_id, "registres_assistencia", "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey("fk_assistencies_alumnes_alumne",
                    x => x.alumne_id, "alumnes", "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex("ix_assistencies_registre_alumne",
            "assistencies_alumnes", new[] { "registre_assistencia_id", "alumne_id" }, unique: true);

        // ── Índexs de rendiment ───────────────────────────
        migrationBuilder.CreateIndex("ix_assistencies_alumne_data",
            "assistencies_alumnes", "alumne_id");
        migrationBuilder.CreateIndex("ix_registres_data",
            "registres_assistencia", "data");
        migrationBuilder.CreateIndex("ix_alumnes_grup",
            "alumnes", "grup_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("assistencies_alumnes");
        migrationBuilder.DropTable("registres_assistencia");
        migrationBuilder.DropTable("franjes_horaries");
        migrationBuilder.DropTable("alumnes");
        migrationBuilder.DropTable("grups");
        migrationBuilder.DropTable("usuaris");
        migrationBuilder.DropTable("cursos");
        migrationBuilder.DropTable("cicles");
        migrationBuilder.DropTable("dies_calendari");
        migrationBuilder.DropTable("anys_academics");
    }
}
