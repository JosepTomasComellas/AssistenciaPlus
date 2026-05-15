namespace AssistenciaPlus.Web.Models;

// ── Wrapper de resposta API ────────────────────────────────
public class ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Error { get; init; }
}

public class ApiResponse
{
    public bool Success { get; init; }
    public string? Error { get; init; }
}

// ── Auth ───────────────────────────────────────────────────
public record LoginRequestModel
{
    public string Email { get; init; } = string.Empty;
    public string Contrasenya { get; init; } = string.Empty;
}

public record LoginResponseModel
{
    public string Token { get; init; } = string.Empty;
    public UsuariModel Usuari { get; init; } = null!;
    public DateTime CaducarAt { get; init; }
}

// ── Usuari ─────────────────────────────────────────────────
public record UsuariModel
{
    public Guid Id { get; init; }
    public string Nom { get; init; } = string.Empty;
    public string Cognom1 { get; init; } = string.Empty;
    public string? Cognom2 { get; init; }
    public string NomComplet { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Rol { get; init; } = string.Empty;
    public string? FotoPath { get; init; }
    public bool EsActiu { get; init; }
    public string Idioma { get; init; } = "ca";
    public DateTime? UltimAcces { get; init; }
}

public record CrearUsuariModel
{
    public string Nom { get; init; } = string.Empty;
    public string Cognom1 { get; init; } = string.Empty;
    public string? Cognom2 { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Rol { get; init; } = "Mestre";
    public string Idioma { get; init; } = "ca";
}

public record ActualitzarUsuariModel
{
    public string Nom { get; init; } = string.Empty;
    public string Cognom1 { get; init; } = string.Empty;
    public string? Cognom2 { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Rol { get; init; } = "Mestre";
    public string Idioma { get; init; } = "ca";
    public bool EsActiu { get; init; }
}

public record CanviContrasenyaModel
{
    public string ContrasenyaActual { get; init; } = string.Empty;
    public string ContrasenyaNova { get; init; } = string.Empty;
}

// ── Any acadèmic ───────────────────────────────────────────
public record AnyAcademicModel
{
    public Guid Id { get; init; }
    public string Nom { get; init; } = string.Empty;
    public DateOnly DataInici { get; init; }
    public DateOnly DataFi { get; init; }
    public bool EsActiu { get; init; }
    public DateOnly IniciFT1 { get; init; }
    public DateOnly FiT1 { get; init; }
    public DateOnly IniciFT2 { get; init; }
    public DateOnly FiT2 { get; init; }
    public DateOnly IniciFT3 { get; init; }
    public DateOnly FiT3 { get; init; }
}

public record CrearAnyAcademicModel
{
    public string Nom { get; init; } = string.Empty;
    public DateOnly DataInici { get; init; }
    public DateOnly DataFi { get; init; }
    public DateOnly IniciT1 { get; init; }
    public DateOnly FiT1 { get; init; }
    public DateOnly IniciT2 { get; init; }
    public DateOnly FiT2 { get; init; }
    public DateOnly IniciT3 { get; init; }
    public DateOnly FiT3 { get; init; }
}

public record CrearGrupModel
{
    public Guid CursId { get; init; }
    public string Lletra { get; init; } = string.Empty;
    public Guid? TutorId { get; init; }
}

public record ActualitzarGrupModel
{
    public string Lletra { get; init; } = string.Empty;
    public Guid? TutorId { get; init; }
}

// ── Grups i cursos ─────────────────────────────────────────
public record GrupModel
{
    public Guid Id { get; init; }
    public string Lletra { get; init; } = string.Empty;
    public string NomComplet { get; init; } = string.Empty;
    public Guid CursId { get; init; }
    public string CursNom { get; init; } = string.Empty;
    public string CursCodi { get; init; } = string.Empty;
    public bool UsaModeFusteta { get; init; }
    public Guid CicleId { get; init; }
    public string CicleNom { get; init; } = string.Empty;
    public Guid AnyAcademicId { get; init; }
    public Guid? TutorId { get; init; }
    public string? TutorNomComplet { get; init; }
    public int NombreAlumnes { get; init; }
}

public record FranjaHorariaModel
{
    public Guid Id { get; init; }
    public string Nom { get; init; } = string.Empty;
    public TimeOnly HoraInici { get; init; }
    public TimeOnly HoraFi { get; init; }
    public bool EsMati { get; init; }
    public bool EsJornadaIntensiva { get; init; }
    public int Ordre { get; init; }

    public string HorariDisplay =>
        $"{HoraInici:HH\\:mm} - {HoraFi:HH\\:mm}";
}

public record CicleModel
{
    public Guid Id { get; init; }
    public string Nom { get; init; } = string.Empty;
    public int Ordre { get; init; }
    public List<CursModel> Cursos { get; init; } = [];
}

public record CursModel
{
    public Guid Id { get; init; }
    public Guid CicleId { get; init; }
    public string Nom { get; init; } = string.Empty;
    public string Codi { get; init; } = string.Empty;
    public int Ordre { get; init; }
    public bool UsaModeFusteta { get; init; }
}

// ── Alumnes ────────────────────────────────────────────────
public record AlumneModel
{
    public Guid Id { get; init; }
    public string Nom { get; init; } = string.Empty;
    public string Cognom1 { get; init; } = string.Empty;
    public string? Cognom2 { get; init; }
    public string NomComplet { get; init; } = string.Empty;
    public string NomFusteta { get; init; } = string.Empty;
    public DateOnly? DataNaixement { get; init; }
    public string? FotoPath { get; init; }

    public Guid GrupId { get; init; }
    public int OrdreFusteta { get; init; }
    public bool EsActiu { get; init; }

    public string Inicials
    {
        get
        {
            var nom = string.IsNullOrWhiteSpace(NomFusteta) ? Nom : NomFusteta;
            if (string.IsNullOrWhiteSpace(nom)) return "?";
            var parts = nom.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2
                ? $"{parts[0][0]}{parts[1][0]}".ToUpper()
                : nom[..Math.Min(2, nom.Length)].ToUpper();
        }
    }
}

public record CrearAlumneModel
{
    public string Nom { get; init; } = string.Empty;
    public string Cognom1 { get; init; } = string.Empty;
    public string? Cognom2 { get; init; }
    public DateOnly? DataNaixement { get; init; }

    public Guid GrupId { get; init; }
    public int OrdreFusteta { get; init; }
}

public record ActualitzarAlumneModel
{
    public string Nom { get; init; } = string.Empty;
    public string Cognom1 { get; init; } = string.Empty;
    public string? Cognom2 { get; init; }
    public DateOnly? DataNaixement { get; init; }

    public int OrdreFusteta { get; init; }
    public bool EsActiu { get; init; }
}

// ── Assistència ────────────────────────────────────────────
public enum EstatAssistencia
{
    Present = 0,
    Absent = 1,
    Tard = 2,
    AbsenciaParcial = 3
}

public enum MotiuAbsencia
{
    SenseMotiu = 0,
    Metge = 1,
    NoEsTrobaBe = 2,
    ServeiExtern = 3,
    FamiliaVeBuscar = 4,
    Tard = 5
}

public record RegistreAssistenciaModel
{
    public Guid Id { get; init; }
    public Guid GrupId { get; init; }
    public string GrupNom { get; init; } = string.Empty;
    public Guid FranjaHorariaId { get; init; }
    public string FranjaNom { get; init; } = string.Empty;
    public DateOnly Data { get; init; }
    public Guid MestreId { get; init; }
    public string MestreNom { get; init; } = string.Empty;
    public string? Observacio { get; init; }
    public DateTime DegatAt { get; init; }
    public List<AssistenciaAlumneModel> Assistencies { get; init; } = [];
}

public record AssistenciaAlumneModel
{
    public Guid Id { get; init; }
    public Guid AlumneId { get; init; }
    public string AlumneNomComplet { get; init; } = string.Empty;
    public string AlumneNomFusteta { get; init; } = string.Empty;
    public int OrdreFusteta { get; init; }
    public EstatAssistencia Estat { get; init; }
    public MotiuAbsencia? Motiu { get; init; }
    public bool? Torna { get; init; }
    public int? MinutsRetard { get; init; }
    public string? Observacio { get; init; }
    public bool AplicatRestaDia { get; init; }
}

public class DesarSessioModel
{
    public Guid GrupId { get; init; }
    public Guid FranjaHorariaId { get; init; }
    public DateOnly Data { get; init; }
    public string? Observacio { get; set; }
    public List<AssistenciaAlumneInputModel> Assistencies { get; init; } = [];
}

public class AssistenciaAlumneInputModel
{
    public Guid AlumneId { get; init; }
    public EstatAssistencia Estat { get; set; } = EstatAssistencia.Present;
    public MotiuAbsencia? Motiu { get; set; }
    public bool? Torna { get; set; }
    public int? MinutsRetard { get; set; }
    public string? Observacio { get; set; }
    public bool AplicarRestaDia { get; set; }
}

// ── Informes ───────────────────────────────────────────────
public record InformeMensualModel
{
    public string AnyAcademic { get; init; } = string.Empty;
    public int Any { get; init; }
    public int Mes { get; init; }
    public string? GrupNom { get; init; }
    public string? CicleNom { get; init; }
    public int TotalAlumnes { get; init; }
    public List<InformeAlumneModel> Alumnes { get; init; } = [];
    public string Titol => GrupNom ?? CicleNom ?? string.Empty;
}

public record InformeTrimestralModel
{
    public string AnyAcademic { get; init; } = string.Empty;
    public int Trimestre { get; init; }
    public DateOnly DataInici { get; init; }
    public DateOnly DataFi { get; init; }
    public string? GrupNom { get; init; }
    public string? CicleNom { get; init; }
    public int TotalAlumnes { get; init; }
    public List<InformeAlumneModel> Alumnes { get; init; } = [];
    public string Titol => GrupNom ?? CicleNom ?? string.Empty;
}

public record InformeAlumneModel
{
    public Guid AlumneId { get; init; }
    public string AlumneNom { get; init; } = string.Empty;
    public string GrupNom { get; init; } = string.Empty;
    public int TotalFrangesPossibles { get; init; }
    public int FrangesPresents { get; init; }
    public int FrangesAbsents { get; init; }
    public int FrangesTard { get; init; }
    public int FrangesAbsenciaParcial { get; init; }
    public decimal PercentatgeAbsencia { get; init; }
    public bool EsAlerta { get; init; }
    public bool EsCritic { get; init; }
    public Dictionary<string, int> DetallMotius { get; init; } = [];
}

public record EnviarInformeModel
{
    public Guid GrupId { get; init; }
    public int Any { get; init; }
    public int Mes { get; init; }
    public bool IncloureAdjuntPdf { get; init; } = true;
}
