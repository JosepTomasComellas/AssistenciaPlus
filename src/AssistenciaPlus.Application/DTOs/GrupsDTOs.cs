using System.ComponentModel.DataAnnotations;

namespace AssistenciaPlus.Application.DTOs;

public record CicleDto
{
    public Guid Id { get; init; }
    public string Nom { get; init; } = string.Empty;
    public int Ordre { get; init; }
    public List<CursDto> Cursos { get; init; } = [];
}

public record CursDto
{
    public Guid Id { get; init; }
    public Guid CicleId { get; init; }
    public string Nom { get; init; } = string.Empty;
    public string Codi { get; init; } = string.Empty;
    public int Ordre { get; init; }
    public bool UsaModeFusteta { get; init; }
}

public record GrupDto
{
    public Guid Id { get; init; }
    public string Lletra { get; init; } = string.Empty;
    public string NomComplet { get; init; } = string.Empty;
    public Guid CursId { get; init; }
    public string CursNom { get; init; } = string.Empty;
    public string CursCodi { get; init; } = string.Empty;
    public bool UsaModeFusteta { get; init; }
    public Guid CicleId { get; init; }
    public string CicleNom { get; init; } = string.Empty;
    public Guid AnyAcademicId { get; init; }
    public Guid? TutorId { get; init; }
    public string? TutorNomComplet { get; init; }
    public int NombreAlumnes { get; init; }
    public List<MestreGrupDto> MestresAutoritzats { get; init; } = [];
}

public record MestreGrupDto
{
    public Guid UsuariId { get; init; }
    public string NomComplet { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public DateTime AfegitAt { get; init; }
}

public record SessioTraçabilitatDto
{
    public Guid Id { get; init; }
    public DateOnly Data { get; init; }
    public string GrupNom { get; init; } = string.Empty;
    public string FranjaNom { get; init; } = string.Empty;
    public string MestreNomComplet { get; init; } = string.Empty;
    public int NombreAbsents { get; init; }
    public int NombrePresents { get; init; }
    public int NombreTard { get; init; }
    public int NombreTotal { get; init; }
    public DateTime DegatAt { get; init; }
}

public record ImportarAlumnesResultDto
{
    public int Importats { get; init; }
    public int Ignorats { get; init; }
    public List<string> Errors { get; init; } = [];
}

public record MigrarAnyAcademicDto
{
    [System.ComponentModel.DataAnnotations.Required]
    public Guid NouAnyAcademicId { get; init; }
    public bool IncloureAlumnes { get; init; } = true;
}

public record ResultatMigracioDto
{
    public int GrupsCopials { get; init; }
    public int AlumnesCopials { get; init; }
}

public record AlumneDto
{
    public Guid Id { get; init; }
    public string Nom { get; init; } = string.Empty;
    public string Cognom1 { get; init; } = string.Empty;
    public string? Cognom2 { get; init; }
    public string NomComplet { get; init; } = string.Empty;
    public string NomFusteta { get; init; } = string.Empty;
    public DateOnly? DataNaixement { get; init; }
    public string? FotoPath { get; init; }

    public Guid GrupId { get; init; }
    public int OrdreFusteta { get; init; }
    public bool EsActiu { get; init; }
}

public record CrearCicleDto
{
    [Required][StringLength(60, MinimumLength = 1)]
    public string Nom { get; init; } = string.Empty;

    [Range(1, 99)]
    public int Ordre { get; init; }
}

public record ActualitzarCicleDto
{
    [Required][StringLength(60, MinimumLength = 1)]
    public string Nom { get; init; } = string.Empty;

    [Range(1, 99)]
    public int Ordre { get; init; }
}

public record CrearCursDto
{
    [Required]
    public Guid CicleId { get; init; }

    [Required][StringLength(60, MinimumLength = 1)]
    public string Nom { get; init; } = string.Empty;

    [Required][StringLength(10, MinimumLength = 1)]
    public string Codi { get; init; } = string.Empty;

    [Range(1, 99)]
    public int Ordre { get; init; }

    public bool UsaModeFusteta { get; init; }
}

public record ActualitzarCursDto
{
    [Required][StringLength(60, MinimumLength = 1)]
    public string Nom { get; init; } = string.Empty;

    [Required][StringLength(10, MinimumLength = 1)]
    public string Codi { get; init; } = string.Empty;

    [Range(1, 99)]
    public int Ordre { get; init; }

    public bool UsaModeFusteta { get; init; }
}

public record CrearGrupDto
{
    [Required]
    public Guid CursId { get; init; }

    [Required][StringLength(3, MinimumLength = 1)]
    public string Lletra { get; init; } = string.Empty;

    public Guid? TutorId { get; init; }
}

public record ActualitzarGrupDto
{
    [Required][StringLength(3, MinimumLength = 1)]
    public string Lletra { get; init; } = string.Empty;

    public Guid? TutorId { get; init; }
}

public record CrearAlumneDto
{
    [Required][StringLength(60, MinimumLength = 1)]
    public string Nom { get; init; } = string.Empty;

    [Required][StringLength(60, MinimumLength = 1)]
    public string Cognom1 { get; init; } = string.Empty;

    [StringLength(60)]
    public string? Cognom2 { get; init; }

    public DateOnly? DataNaixement { get; init; }

    [Required]
    public Guid GrupId { get; init; }

    [Range(0, 999)]
    public int OrdreFusteta { get; init; }
}

public record ActualitzarAlumneDto
{
    public string Nom { get; init; } = string.Empty;
    public string Cognom1 { get; init; } = string.Empty;
    public string? Cognom2 { get; init; }
    public DateOnly? DataNaixement { get; init; }

    public int OrdreFusteta { get; init; }
    public bool EsActiu { get; init; }
}

public record FranjaHorariaDto
{
    public Guid Id { get; init; }
    public string Nom { get; init; } = string.Empty;
    public TimeOnly HoraInici { get; init; }
    public TimeOnly HoraFi { get; init; }
    public bool EsMati { get; init; }
    public bool EsJornadaIntensiva { get; init; }
    public int Ordre { get; init; }
}

public record UsuariDto
{
    public Guid Id { get; init; }
    public string Nom { get; init; } = string.Empty;
    public string Cognom1 { get; init; } = string.Empty;
    public string? Cognom2 { get; init; }
    public string NomComplet { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Rol { get; init; } = string.Empty;
    public string? FotoPath { get; init; }
    public bool EsActiu { get; init; }
    public string Idioma { get; init; } = "ca";
    public DateTime? UltimAcces { get; init; }
}

public record CrearUsuariDto
{
    [Required][StringLength(60, MinimumLength = 1)]
    public string Nom { get; init; } = string.Empty;

    [Required][StringLength(60, MinimumLength = 1)]
    public string Cognom1 { get; init; } = string.Empty;

    [StringLength(60)]
    public string? Cognom2 { get; init; }

    [Required][EmailAddress][StringLength(120)]
    public string Email { get; init; } = string.Empty;

    [Required][RegularExpression("Mestre|EquipDirectiu|Administratiu")]
    public string Rol { get; init; } = "Mestre";

    [Required][RegularExpression("ca|es")]
    public string Idioma { get; init; } = "ca";
}

public record ActualitzarUsuariDto
{
    [Required][StringLength(60, MinimumLength = 1)]
    public string Nom { get; init; } = string.Empty;

    [Required][StringLength(60, MinimumLength = 1)]
    public string Cognom1 { get; init; } = string.Empty;

    [StringLength(60)]
    public string? Cognom2 { get; init; }

    [Required][EmailAddress][StringLength(120)]
    public string Email { get; init; } = string.Empty;

    [Required][RegularExpression("Mestre|EquipDirectiu|Administratiu")]
    public string Rol { get; init; } = "Mestre";

    [Required][RegularExpression("ca|es")]
    public string Idioma { get; init; } = "ca";

    public bool EsActiu { get; init; }
}

public record LoginRequestDto
{
    [Required][EmailAddress][StringLength(120)]
    public string Email { get; init; } = string.Empty;

    [Required][StringLength(100, MinimumLength = 6)]
    public string Contrasenya { get; init; } = string.Empty;
}

public record LoginResponseDto
{
    public string Token { get; init; } = string.Empty;
    public UsuariDto Usuari { get; init; } = null!;
    public DateTime CaducarAt { get; init; }
}

public record CanviContrasenyaDto
{
    [Required][StringLength(100, MinimumLength = 1)]
    public string ContrasenyaActual { get; init; } = string.Empty;

    [Required][StringLength(100, MinimumLength = 12)]
    public string ContrasenyaNova { get; init; } = string.Empty;
}
