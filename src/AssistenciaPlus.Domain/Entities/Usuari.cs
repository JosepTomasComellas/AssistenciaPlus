namespace AssistenciaPlus.Domain.Entities;

/// <summary>
/// Rols d'usuari del sistema.
/// </summary>
public enum RolUsuari
{
    Mestre = 0,          // Passa llista als seus grups
    EquipDirectiu = 1,   // Passa llista + modifica altres + configuració
    Administratiu = 2    // Visualitza informes, no modifica
}

/// <summary>
/// Usuari del sistema (mestre, equip directiu o administratiu).
/// </summary>
public class Usuari : BaseEntity
{
    public string Nom { get; set; } = string.Empty;
    public string Cognom1 { get; set; } = string.Empty;
    public string? Cognom2 { get; set; }

    public string NomComplet => $"{Nom} {Cognom1} {Cognom2 ?? string.Empty}".Trim();

    public string Email { get; set; } = string.Empty;

    /// <summary>Hash de la contrasenya (BCrypt)</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public RolUsuari Rol { get; set; } = RolUsuari.Mestre;

    /// <summary>Ruta relativa a la foto de perfil</summary>
    public string? FotoPath { get; set; }

    public bool EsActiu { get; set; } = true;

    /// <summary>Idioma preferit: "ca" o "es"</summary>
    public string Idioma { get; set; } = "ca";

    /// <summary>Últim accés al sistema</summary>
    public DateTime? UltimAcces { get; set; }

    // Navegació
    public ICollection<Grup> GrupsTutoritzats { get; set; } = new List<Grup>();
    public ICollection<RegistreAssistencia> RegistresPassats { get; set; } = new List<RegistreAssistencia>();
    public ICollection<GrupMestre> GrupsAutoritzats { get; set; } = new List<GrupMestre>();
}
