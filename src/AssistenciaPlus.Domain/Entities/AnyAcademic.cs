namespace AssistenciaPlus.Domain.Entities;

/// <summary>
/// Representa un any acadèmic (p.ex. 2025-2026).
/// Conté la configuració del calendari escolar per a aquest any.
/// </summary>
public class AnyAcademic : BaseEntity
{
    /// <summary>Nom descriptiu, p.ex. "2025-2026"</summary>
    public string Nom { get; set; } = string.Empty;

    public DateOnly DataInici { get; set; }
    public DateOnly DataFi { get; set; }

    /// <summary>Any acadèmic actiu en curs</summary>
    public bool EsActiu { get; set; } = false;

    // Definició dels trimestres (mapa BD: inici_t1, fi_t1, inici_t2, fi_t2, inici_t3, fi_t3)
    public DateOnly IniciT1 { get; set; }
    public DateOnly FiT1 { get; set; }
    public DateOnly IniciT2 { get; set; }
    public DateOnly FiT2 { get; set; }
    public DateOnly IniciT3 { get; set; }
    public DateOnly FiT3 { get; set; }

    // Navegació
    public ICollection<DiaCalendari> DiesCalendari { get; set; } = new List<DiaCalendari>();
    public ICollection<Grup> Grups { get; set; } = new List<Grup>();
}
