using System;
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
            migrationBuilder.CreateTable(
                name: "anys_academics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nom = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    data_inici = table.Column<DateOnly>(type: "date", nullable: false),
                    data_fi = table.Column<DateOnly>(type: "date", nullable: false),
                    es_actiu = table.Column<bool>(type: "boolean", nullable: false),
                    inici_t1 = table.Column<DateOnly>(type: "date", nullable: false),
                    fi_t1 = table.Column<DateOnly>(type: "date", nullable: false),
                    inici_t2 = table.Column<DateOnly>(type: "date", nullable: false),
                    fi_t2 = table.Column<DateOnly>(type: "date", nullable: false),
                    inici_t3 = table.Column<DateOnly>(type: "date", nullable: false),
                    fi_t3 = table.Column<DateOnly>(type: "date", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_anys_academics", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cicles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nom = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    ordre = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cicles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "franjes_horaries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nom = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    hora_inici = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    hora_fi = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    es_mati = table.Column<bool>(type: "boolean", nullable: false),
                    es_jornada_intensiva = table.Column<bool>(type: "boolean", nullable: false),
                    ordre = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_franjes_horaries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "usuaris",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nom = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    cognom1 = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    cognom2 = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    rol = table.Column<int>(type: "integer", nullable: false),
                    foto_path = table.Column<string>(type: "text", nullable: true),
                    es_actiu = table.Column<bool>(type: "boolean", nullable: false),
                    idioma = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false, defaultValue: "ca"),
                    ultim_acces = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usuaris", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dies_calendari",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    any_academic_id = table.Column<Guid>(type: "uuid", nullable: false),
                    data = table.Column<DateOnly>(type: "date", nullable: false),
                    tipus_dia = table.Column<int>(type: "integer", nullable: false),
                    descripcio = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dies_calendari", x => x.id);
                    table.ForeignKey(
                        name: "fk_dies_calendari_anys_academics_any_academic_id",
                        column: x => x.any_academic_id,
                        principalTable: "anys_academics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cursos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nom = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    codi = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ordre = table.Column<int>(type: "integer", nullable: false),
                    usa_mode_fusteta = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cursos", x => x.id);
                    table.ForeignKey(
                        name: "fk_cursos_cicles_cicle_id",
                        column: x => x.cicle_id,
                        principalTable: "cicles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "grups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    curs_id = table.Column<Guid>(type: "uuid", nullable: false),
                    any_academic_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lletra = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    tutor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_grups", x => x.id);
                    table.ForeignKey(
                        name: "fk_grups_anys_academics_any_academic_id",
                        column: x => x.any_academic_id,
                        principalTable: "anys_academics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_grups_cursos_curs_id",
                        column: x => x.curs_id,
                        principalTable: "cursos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_grups_usuaris_tutor_id",
                        column: x => x.tutor_id,
                        principalTable: "usuaris",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "alumnes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nom = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    cognom1 = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    cognom2 = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    data_naixement = table.Column<DateOnly>(type: "date", nullable: true),
                    foto_path = table.Column<string>(type: "text", nullable: true),
                    grup_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ordre_fusteta = table.Column<int>(type: "integer", nullable: false),
                    es_actiu = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_alumnes", x => x.id);
                    table.ForeignKey(
                        name: "fk_alumnes_grups_grup_id",
                        column: x => x.grup_id,
                        principalTable: "grups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "registres_assistencia",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    grup_id = table.Column<Guid>(type: "uuid", nullable: false),
                    franja_horaria_id = table.Column<Guid>(type: "uuid", nullable: false),
                    data = table.Column<DateOnly>(type: "date", nullable: false),
                    mestre_id = table.Column<Guid>(type: "uuid", nullable: false),
                    degat_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    observacio = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_registres_assistencia", x => x.id);
                    table.ForeignKey(
                        name: "fk_registres_assistencia_franjes_horaries_franja_horaria_id",
                        column: x => x.franja_horaria_id,
                        principalTable: "franjes_horaries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registres_assistencia_grups_grup_id",
                        column: x => x.grup_id,
                        principalTable: "grups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registres_assistencia_usuaris_mestre_id",
                        column: x => x.mestre_id,
                        principalTable: "usuaris",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "assistencies_alumnes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    registre_assistencia_id = table.Column<Guid>(type: "uuid", nullable: false),
                    alumne_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estat = table.Column<int>(type: "integer", nullable: false),
                    motiu = table.Column<int>(type: "integer", nullable: true),
                    torna = table.Column<bool>(type: "boolean", nullable: true),
                    minuts_retard = table.Column<int>(type: "integer", nullable: true),
                    observacio = table.Column<string>(type: "text", nullable: true),
                    aplicat_resta_dia = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_assistencies_alumnes", x => x.id);
                    table.ForeignKey(
                        name: "fk_assistencies_alumnes_alumnes_alumne_id",
                        column: x => x.alumne_id,
                        principalTable: "alumnes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_assistencies_alumnes_registres_assistencia_registre_assiste",
                        column: x => x.registre_assistencia_id,
                        principalTable: "registres_assistencia",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_alumnes_grup_id",
                table: "alumnes",
                column: "grup_id");

            migrationBuilder.CreateIndex(
                name: "ix_anys_academics_es_actiu",
                table: "anys_academics",
                column: "es_actiu");

            migrationBuilder.CreateIndex(
                name: "ix_assistencies_alumnes_alumne_id",
                table: "assistencies_alumnes",
                column: "alumne_id");

            migrationBuilder.CreateIndex(
                name: "ix_assistencies_alumnes_registre_assistencia_id_alumne_id",
                table: "assistencies_alumnes",
                columns: new[] { "registre_assistencia_id", "alumne_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cursos_cicle_id",
                table: "cursos",
                column: "cicle_id");

            migrationBuilder.CreateIndex(
                name: "ix_dies_calendari_any_academic_id_data",
                table: "dies_calendari",
                columns: new[] { "any_academic_id", "data" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_grups_any_academic_id",
                table: "grups",
                column: "any_academic_id");

            migrationBuilder.CreateIndex(
                name: "ix_grups_curs_id_any_academic_id_lletra",
                table: "grups",
                columns: new[] { "curs_id", "any_academic_id", "lletra" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_grups_tutor_id",
                table: "grups",
                column: "tutor_id");

            migrationBuilder.CreateIndex(
                name: "ix_registres_assistencia_franja_horaria_id",
                table: "registres_assistencia",
                column: "franja_horaria_id");

            migrationBuilder.CreateIndex(
                name: "ix_registres_assistencia_grup_id_data",
                table: "registres_assistencia",
                columns: new[] { "grup_id", "data" });

            migrationBuilder.CreateIndex(
                name: "ix_registres_assistencia_grup_id_franja_horaria_id_data",
                table: "registres_assistencia",
                columns: new[] { "grup_id", "franja_horaria_id", "data" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_registres_assistencia_mestre_id",
                table: "registres_assistencia",
                column: "mestre_id");

            migrationBuilder.CreateIndex(
                name: "ix_usuaris_email",
                table: "usuaris",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assistencies_alumnes");

            migrationBuilder.DropTable(
                name: "dies_calendari");

            migrationBuilder.DropTable(
                name: "alumnes");

            migrationBuilder.DropTable(
                name: "registres_assistencia");

            migrationBuilder.DropTable(
                name: "franjes_horaries");

            migrationBuilder.DropTable(
                name: "grups");

            migrationBuilder.DropTable(
                name: "anys_academics");

            migrationBuilder.DropTable(
                name: "cursos");

            migrationBuilder.DropTable(
                name: "usuaris");

            migrationBuilder.DropTable(
                name: "cicles");
        }
    }
}
