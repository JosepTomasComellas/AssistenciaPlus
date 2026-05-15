namespace AssistenciaPlus.Domain.Entities;

/// <summary>
/// Estat d'assistència d'un alumne en una franja.
/// </summary>
public enum EstatAssistencia
{
    Present = 0,
    Absent = 1,
    Tard = 2,
    AbsenciaParcial = 3   // Surt o arriba a mig franja
}

/// <summary>
/// Motiu d'una absència o absència parcial.
/// </summary>
public enum MotiuAbsencia
{
    SenseMotiu = 0,
    Metge = 1,
    NoEsTrobaBe = 2,
    ServeiExtern = 3,    // FCT, activitat fora del centre, etc.
    FamiliaVeBuscar = 4,
    Tard = 5             // Motiu específic per a retards
}

/// <summary>
/// Registre d'assistència d'un grup en una data i franja concreta.
/// Un registre agrupa totes les assistències individuals de la sessió.
/// </summary>
public class RegistreAssistencia : BaseEntity
{
    public Guid GrupId { get; set; }
    public Grup Grup { get; set; } = null!;

    public Guid FranjaHorariaId { get; set; }
    public FranjaHoraria FranjaHoraria { get; set; } = null!;

    public DateOnly Data { get; set; }

    /// <summary>Mestre/a que ha passat llista</summary>
    public Guid MestreId { get; set; }
    public Usuari Mestre { get; set; } = null!;

    /// <summary>Data i hora en que s'ha desat el registre</summary>
    public DateTime DegatAt { get; set; } = DateTime.UtcNow;

    /// <summary>Observació general de la sessió (opcional)</summary>
    public string? Observacio { get; set; }

    // Navegació
    public ICollection<AssistenciaAlumne> Assistencies { get; set; } = new List<AssistenciaAlumne>();
}

/// <summary>
/// Assistència individual d'un alumne en un registre de sessió.
/// </summary>
public class AssistenciaAlumne : BaseEntity
{
    public Guid RegistreAssistenciaId { get; set; }
    public RegistreAssistencia RegistreAssistencia { get; set; } = null!;

    public Guid AlumneId { get; set; }
    public Alumne Alumne { get; set; } = null!;

    public EstatAssistencia Estat { get; set; } = EstatAssistencia.Present;

    public MotiuAbsencia? Motiu { get; set; }

    /// <summary>
    /// Per a absències parcials: indica si l'alumne torna durant el dia.
    /// Null = no aplicable, True = torna, False = no torna.
    /// </summary>
    public bool? Torna { get; set; }

    /// <summary>
    /// Per a retards: minuts de retard aproximats.
    /// </summary>
    public int? MinutsRetard { get; set; }

    /// <summary>Observació específica d'aquest alumne en aquesta sessió</summary>
    public string? Observacio { get; set; }

    /// <summary>
    /// Per a absències parcials que afecten la resta del dia:
    /// referència a les altres franges marcades automàticament.
    /// </summary>
    public bool AplicatRestaDia { get; set; } = false;
}
