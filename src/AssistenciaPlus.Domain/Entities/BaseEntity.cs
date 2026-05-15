namespace AssistenciaPlus.Domain.Entities;

/// <summary>
/// Entitat base amb camps d'auditoria comuns a totes les entitats.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
}
