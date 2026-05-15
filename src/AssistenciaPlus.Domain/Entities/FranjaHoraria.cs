namespace AssistenciaPlus.Domain.Entities;

/// <summary>
/// Franja horària oficial de l'escola.
/// Les franges són fixes i configurades al seed inicial.
/// </summary>
public class FranjaHoraria : BaseEntity
{
    /// <summary>Nom descriptiu (p.ex. "Matí 1", "Pati", "Tarda 1")</summary>
    public string Nom { get; set; } = string.Empty;

    public TimeOnly HoraInici { get; set; }
    public TimeOnly HoraFi { get; set; }

    /// <summary>Durada en minuts calculada</summary>
    public int DuradaMinuts => (int)(HoraFi - HoraInici).TotalMinutes;

    /// <summary>Indica si és franja de matí (true) o tarda (false)</summary>
    public bool EsMati { get; set; }

    /// <summary>
    /// Indica si aquesta franja es fa en jornada intensiva.
    /// Totes les franges de matí = true. Tarda = false.
    /// </summary>
    public bool EsJornadaIntensiva { get; set; }

    /// <summary>Ordre de visualització</summary>
    public int Ordre { get; set; }

    // Navegació
    public ICollection<RegistreAssistencia> RegistresAssistencia { get; set; } = new List<RegistreAssistencia>();
}
