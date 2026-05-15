using AssistenciaPlus.Domain.Entities;

namespace AssistenciaPlus.Application.DTOs;

public record AnyAcademicDto
{
    public Guid Id { get; init; }
    public string Nom { get; init; } = string.Empty;
    public DateOnly DataInici { get; init; }
    public DateOnly DataFi { get; init; }
    public bool EsActiu { get; init; }
    public DateOnly IniciFT1 { get; init; }
    public DateOnly FiT1 { get; init; }
    public DateOnly IniciFT2 { get; init; }
    public DateOnly FiT2 { get; init; }
    public DateOnly IniciFT3 { get; init; }
    public DateOnly FiT3 { get; init; }
}

public record DiaCalendariDto
{
    public Guid Id { get; init; }
    public Guid AnyAcademicId { get; init; }
    public DateOnly Data { get; init; }
    public TipusDia TipusDia { get; init; }
    public string? Descripcio { get; init; }
}

public record ActualitzarDiaCalendariDto
{
    public Guid AnyAcademicId { get; init; }
    public DateOnly Data { get; init; }
    public TipusDia TipusDia { get; init; }
    public string? Descripcio { get; init; }
}

public record ConfigurarDiesDto
{
    public Guid AnyAcademicId { get; init; }
    public List<ActualitzarDiaCalendariDto> Dies { get; init; } = [];
}

public record CrearAnyAcademicDto
{
    public string Nom { get; init; } = string.Empty;
    public DateOnly DataInici { get; init; }
    public DateOnly DataFi { get; init; }
    public DateOnly IniciT1 { get; init; }
    public DateOnly FiT1 { get; init; }
    public DateOnly IniciT2 { get; init; }
    public DateOnly FiT2 { get; init; }
    public DateOnly IniciT3 { get; init; }
    public DateOnly FiT3 { get; init; }
}
