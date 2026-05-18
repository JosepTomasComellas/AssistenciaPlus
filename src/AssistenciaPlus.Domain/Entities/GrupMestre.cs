namespace AssistenciaPlus.Domain.Entities;

/// <summary>
/// Taula de relació many-to-many entre Grup i Usuari (mestres autoritzats a passar llista).
/// El tutor principal ja té accés per TutorId; aquesta taula afegeix mestres addicionals.
/// </summary>
public class GrupMestre
{
    public Guid GrupId { get; set; }
    public Grup Grup { get; set; } = null!;

    public Guid UsuariId { get; set; }
    public Usuari Usuari { get; set; } = null!;

    public DateTime AfegitAt { get; set; } = DateTime.UtcNow;
}
