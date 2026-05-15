namespace AssistenciaPlus.Domain.Entities;

/// <summary>
/// Grup/classe dins d'un curs per a un any acadèmic concret.
/// Ex: 1r A 2025-2026, 1r B 2025-2026.
/// Infantil només té un grup per curs (sense lletra).
/// </summary>
public class Grup : BaseEntity
{
    public Guid CursId { get; set; }
    public Curs Curs { get; set; } = null!;

    public Guid AnyAcademicId { get; set; }
    public AnyAcademic AnyAcademic { get; set; } = null!;

    /// <summary>Lletra del grup: "A", "B" o buit per a infantil</summary>
    public string Lletra { get; set; } = string.Empty;

    /// <summary>Nom complet generat: "1r A", "I3", etc.</summary>
    public string NomComplet => string.IsNullOrEmpty(Lletra)
        ? Curs?.Nom ?? string.Empty
        : $"{Curs?.Nom} {Lletra}";

    /// <summary>Tutor/a principal del grup</summary>
    public Guid? TutorId { get; set; }
    public Usuari? Tutor { get; set; }

    // Navegació
    public ICollection<Alumne> Alumnes { get; set; } = new List<Alumne>();
    public ICollection<RegistreAssistencia> RegistresAssistencia { get; set; } = new List<RegistreAssistencia>();
}
