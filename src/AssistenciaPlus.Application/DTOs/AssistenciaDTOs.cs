using AssistenciaPlus.Domain.Entities;

namespace AssistenciaPlus.Application.DTOs;

public record RegistreAssistenciaDto
{
    public Guid Id { get; init; }
    public Guid GrupId { get; init; }
    public string GrupNom { get; init; } = string.Empty;
    public Guid FranjaHorariaId { get; init; }
    public string FranjaNom { get; init; } = string.Empty;
    public DateOnly Data { get; init; }
    public Guid MestreId { get; init; }
    public string MestreNom { get; init; } = string.Empty;
    public string? Observacio { get; init; }
    public DateTime DegatAt { get; init; }
    public List<AssistenciaAlumneDto> Assistencies { get; init; } = [];
}

public record AssistenciaAlumneDto
{
    public Guid Id { get; init; }
    public Guid AlumneId { get; init; }
    public string AlumneNomComplet { get; init; } = string.Empty;
    public string AlumneNomFusteta { get; init; } = string.Empty;
    public int OrdreFusteta { get; init; }
    public EstatAssistencia Estat { get; init; }
    public MotiuAbsencia? Motiu { get; init; }
    public bool? Torna { get; init; }
    public int? MinutsRetard { get; init; }
    public string? Observacio { get; init; }
    public bool AplicatRestaDia { get; init; }
}

public record DesarSessioDto
{
    public Guid GrupId { get; init; }
    public Guid FranjaHorariaId { get; init; }
    public DateOnly Data { get; init; }
    public string? Observacio { get; init; }
    public List<AssistenciaAlumneInputDto> Assistencies { get; init; } = [];
}

public record AssistenciaAlumneInputDto
{
    public Guid AlumneId { get; init; }
    public EstatAssistencia Estat { get; init; }
    public MotiuAbsencia? Motiu { get; init; }
    public bool? Torna { get; init; }
    public int? MinutsRetard { get; init; }
    public string? Observacio { get; init; }
    public bool AplicarRestaDia { get; init; }
}

public record AplicarAbsenciaParcialDto
{
    public Guid AlumneId { get; init; }
    public Guid GrupId { get; init; }
    public DateOnly Data { get; init; }
    public Guid FranjaActualId { get; init; }
    public MotiuAbsencia Motiu { get; init; }
    public bool Torna { get; init; }
}

public record RetardDto
{
    public Guid AlumneId { get; init; }
    public Guid RegistreAssistenciaId { get; init; }
    public MotiuAbsencia Motiu { get; init; }
    public int MinutsRetard { get; init; }
    public string? Observacio { get; init; }
}
