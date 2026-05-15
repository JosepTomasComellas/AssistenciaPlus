using AssistenciaPlus.Application.DTOs;
using AssistenciaPlus.Domain.Entities;

namespace AssistenciaPlus.Application.Interfaces;

public interface IAssistenciaService
{
    Task<RegistreAssistenciaDto> DesarSessioCompletaAsync(DesarSessioDto dto, Guid mestreId, CancellationToken ct = default);
    Task AplicarAbsenciaParcialRestaDiaAsync(AplicarAbsenciaParcialDto dto, Guid mestreId, CancellationToken ct = default);
    Task<decimal> CalcularPercentatgeAbsenciaAsync(Guid alumneId, Guid anyAcademicId, DateOnly dataInici, DateOnly dataFi, CancellationToken ct = default);
    Task<RegistreAssistenciaDto?> GetRegistreAsync(Guid grupId, Guid franjaId, DateOnly data, CancellationToken ct = default);
    Task<IReadOnlyList<RegistreAssistenciaDto>> GetRegistresPerDataAsync(Guid grupId, DateOnly data, CancellationToken ct = default);
}

public interface IInformesService
{
    Task<InformeMensualDto> GenerarInformeMensualGrupAsync(Guid grupId, int any, int mes, CancellationToken ct = default);
    Task<InformeMensualDto> GenerarInformeMensualCicleAsync(Guid cicleId, int any, int mes, CancellationToken ct = default);
    Task<InformeTrimestralDto> GenerarInformeTrimestralGrupAsync(Guid grupId, int trimestre, Guid anyAcademicId, CancellationToken ct = default);
    Task<InformeTrimestralDto> GenerarInformeTrimestralCicleAsync(Guid cicleId, int trimestre, Guid anyAcademicId, CancellationToken ct = default);
}
