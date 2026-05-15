namespace AssistenciaPlus.Domain.Entities;

/// <summary>
/// Cicle educatiu de l'escola.
/// Ex: Cicle Infantil, Cicle Inicial, Cicle Mitjà, Cicle Superior.
/// </summary>
public class Cicle : BaseEntity
{
    public string Nom { get; set; } = string.Empty;

    /// <summary>Ordre de visualització en informes</summary>
    public int Ordre { get; set; }

    // Navegació
    public ICollection<Curs> Cursos { get; set; } = new List<Curs>();
}

/// <summary>
/// Curs dins d'un cicle.
/// Ex: Infantil 3, Infantil 4, Primer, Segon...
/// </summary>
public class Curs : BaseEntity
{
    public Guid CicleId { get; set; }
    public Cicle Cicle { get; set; } = null!;

    public string Nom { get; set; } = string.Empty;

    /// <summary>Codi curt per a informes (p.ex. "I3", "1r", "2n")</summary>
    public string Codi { get; set; } = string.Empty;

    /// <summary>Ordre dins del cicle</summary>
    public int Ordre { get; set; }

    /// <summary>
    /// Indica si aquest curs usa el mode Fusteta Digital per defecte.
    /// True per a I3, I4, I5 i 1r.
    /// </summary>
    public bool UsaModeFusteta { get; set; } = false;

    // Navegació
    public ICollection<Grup> Grups { get; set; } = new List<Grup>();
}
