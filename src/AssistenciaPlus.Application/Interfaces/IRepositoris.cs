using AssistenciaPlus.Domain.Entities;

namespace AssistenciaPlus.Application.Interfaces;

public interface IAlumneRepository
{
    Task<Alumne?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Alumne>> GetPerGrupAsync(Guid grupId, CancellationToken ct = default);
    Task<Alumne> AfegirAsync(Alumne alumne, CancellationToken ct = default);
    Task ActualitzarAsync(Alumne alumne, CancellationToken ct = default);
    Task EsborrarAsync(Guid id, CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public interface IGrupRepository
{
    Task<Grup?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Grup?> GetByIdAmbDetallsAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Grup>> GetPerMestreAsync(Guid mestreId, Guid anyAcademicId, CancellationToken ct = default);
    Task<IReadOnlyList<Grup>> GetPerAnyAcademicAsync(Guid anyAcademicId, CancellationToken ct = default);
    Task<IReadOnlyList<Grup>> GetPerCicleAsync(Guid cicleId, Guid anyAcademicId, CancellationToken ct = default);
    Task<IReadOnlyList<Cicle>> GetCiclesAsync(CancellationToken ct = default);
    Task<Grup> AfegirAsync(Grup grup, CancellationToken ct = default);
    Task ActualitzarAsync(Grup grup, CancellationToken ct = default);
    Task EsborrarAsync(Guid id, CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    // Cicles
    Task<Cicle?> GetCiclePerIdAsync(Guid id, CancellationToken ct = default);
    Task<Cicle> AfegirCicleAsync(Cicle cicle, CancellationToken ct = default);
    Task ActualitzarCicleAsync(Cicle cicle, CancellationToken ct = default);
    Task EsborrarCicleAsync(Guid id, CancellationToken ct = default);

    // Cursos
    Task<Curs?> GetCursPerIdAsync(Guid id, CancellationToken ct = default);
    Task<Curs> AfegirCursAsync(Curs curs, CancellationToken ct = default);
    Task ActualitzarCursAsync(Curs curs, CancellationToken ct = default);
    Task EsborrarCursAsync(Guid id, CancellationToken ct = default);

    // Mestres autoritzats
    Task<IReadOnlyList<Usuari>> GetMestresGrupAsync(Guid grupId, CancellationToken ct = default);
    Task<bool> EsMestreAutoritzatAsync(Guid grupId, Guid mestreId, CancellationToken ct = default);
    Task AfegirMestreGrupAsync(Guid grupId, Guid mestreId, CancellationToken ct = default);
    Task TreureMestreGrupAsync(Guid grupId, Guid mestreId, CancellationToken ct = default);
}

public interface IAssistenciaRepository
{
    Task<RegistreAssistencia?> GetRegistreAsync(Guid grupId, Guid franjaId, DateOnly data, CancellationToken ct = default);
    Task<RegistreAssistencia?> GetRegistreAmbAssistenciesAsync(Guid grupId, Guid franjaId, DateOnly data, CancellationToken ct = default);
    Task<IReadOnlyList<RegistreAssistencia>> GetRegistresPerGrupIDataAsync(Guid grupId, DateOnly data, CancellationToken ct = default);
    Task<IReadOnlyList<AssistenciaAlumne>> GetAssistenciesAlumneAsync(Guid alumneId, DateOnly dataInici, DateOnly dataFi, CancellationToken ct = default);
    Task<IReadOnlyList<AssistenciaAlumne>> GetAssistenciesGrupMesAsync(Guid grupId, int any, int mes, CancellationToken ct = default);
    Task<IReadOnlyList<AssistenciaAlumne>> GetAssistenciesGrupRangAsync(Guid grupId, DateOnly dataInici, DateOnly dataFi, CancellationToken ct = default);
    Task<IReadOnlyList<RegistreAssistencia>> GetSessionsAsync(Guid anyAcademicId, int pagina, int midaPagina, CancellationToken ct = default);
    Task<int> GetSessionsCountAsync(Guid anyAcademicId, CancellationToken ct = default);
    Task<RegistreAssistencia> AfegirRegistreAsync(RegistreAssistencia registre, CancellationToken ct = default);
    Task ActualitzarRegistreAsync(RegistreAssistencia registre, CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public interface IUsuariRepository
{
    Task<Usuari?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Usuari?> GetPerEmailAsync(string email, CancellationToken ct = default);
    Task<IReadOnlyList<Usuari>> GetTotsAsync(CancellationToken ct = default);
    Task<Usuari> AfegirAsync(Usuari usuari, CancellationToken ct = default);
    Task ActualitzarAsync(Usuari usuari, CancellationToken ct = default);
    Task EsborrarAsync(Guid id, CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public interface ICalendariRepository
{
    Task<AnyAcademic?> GetAnyAcademicActiuAsync(CancellationToken ct = default);
    Task<AnyAcademic?> GetAnyAcademicPerIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<AnyAcademic>> GetAnysAcademicsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<DiaCalendari>> GetDiesRangAsync(Guid anyAcademicId, DateOnly dataInici, DateOnly dataFi, CancellationToken ct = default);
    Task<DiaCalendari?> GetDiaAsync(Guid anyAcademicId, DateOnly data, CancellationToken ct = default);
    Task<IReadOnlyList<FranjaHoraria>> GetFranjesAsync(CancellationToken ct = default);
    Task AfegirDiesAsync(IEnumerable<DiaCalendari> dies, CancellationToken ct = default);
    Task ActualitzarDiaAsync(DiaCalendari dia, CancellationToken ct = default);
    Task EsborrarDiaAsync(Guid anyAcademicId, DateOnly data, CancellationToken ct = default);
    Task<AnyAcademic> AfegirAnyAcademicAsync(AnyAcademic any, CancellationToken ct = default);
    Task ActualitzarAnyAcademicAsync(AnyAcademic any, CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
