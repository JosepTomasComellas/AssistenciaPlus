namespace AssistenciaPlus.Domain.Entities;

/// <summary>
/// Alumne matriculat a l'escola.
/// </summary>
public class Alumne : BaseEntity
{
    public string Nom { get; set; } = string.Empty;
    public string Cognom1 { get; set; } = string.Empty;
    public string? Cognom2 { get; set; }

    /// <summary>Nom complet per a visualització</summary>
    public string NomComplet => $"{Cognom1} {Cognom2 ?? string.Empty} {Nom}".Trim();

    /// <summary>Nom per a la Fusteta Digital (en majúscules)</summary>
    public string NomFusteta => Nom.ToUpperInvariant();

    public DateOnly? DataNaixement { get; set; }

    /// <summary>Ruta relativa a la foto de l'alumne (opcional)</summary>
    public string? FotoPath { get; set; }

    public Guid GrupId { get; set; }
    public Grup Grup { get; set; } = null!;

    /// <summary>Ordre de pas a la Fusteta Digital</summary>
    public int OrdreFusteta { get; set; }

    public bool EsActiu { get; set; } = true;

    // Navegació
    public ICollection<AssistenciaAlumne> Assistencies { get; set; } = new List<AssistenciaAlumne>();
}
