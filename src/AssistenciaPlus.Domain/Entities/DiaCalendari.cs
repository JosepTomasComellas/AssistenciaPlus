namespace AssistenciaPlus.Domain.Entities;

/// <summary>
/// Tipus de dia en el calendari escolar.
/// </summary>
public enum TipusDia
{
    Lectiu = 0,
    Festiu = 1,
    JornadaIntensiva = 2,   // Només matí
    NoLectiu = 3            // Vacances, ponts, etc.
}

/// <summary>
/// Representa cada dia del calendari escolar d'un any acadèmic.
/// Permet calcular correctament els percentatges d'absència.
/// </summary>
public class DiaCalendari : BaseEntity
{
    public Guid AnyAcademicId { get; set; }
    public AnyAcademic AnyAcademic { get; set; } = null!;

    public DateOnly Data { get; set; }
    public TipusDia TipusDia { get; set; } = TipusDia.Lectiu;

    /// <summary>Descripció opcional (p.ex. "Sant Jordi", "Claustre")</summary>
    public string? Descripcio { get; set; }
}
