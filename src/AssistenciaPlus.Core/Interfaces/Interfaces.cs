using AssistenciaPlus.Core.Entities;

namespace AssistenciaPlus.Core.Interfaces;

// ═══════════════════════════════════════════════════════════
//  REPOSITORI GENÈRIC
// ═══════════════════════════════════════════════════════════

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
    Task<T> AddAsync(T entity, CancellationToken ct = default);
    Task UpdateAsync(T entity, CancellationToken ct = default);
    Task DeleteAsync(T entity, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
}

// ═══════════════════════════════════════════════════════════
//  UNIT OF WORK
// ═══════════════════════════════════════════════════════════

public interface IUnitOfWork : IDisposable
{
    IRepository<AcademicYear> AcademicYears { get; }
    IRepository<Cycle> Cycles { get; }
    IRepository<CourseYear> CourseYears { get; }
    IRepository<Group> Groups { get; }
    IRepository<Student> Students { get; }
    IRepository<User> Users { get; }
    IRepository<Schedule> Schedules { get; }
    IRepository<Subject> Subjects { get; }
    IRepository<AttendanceSession> AttendanceSessions { get; }
    IRepository<AttendanceRecord> AttendanceRecords { get; }
    IRepository<AttendanceStatus> AttendanceStatuses { get; }
    IRepository<HolidayDay> HolidayDays { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitTransactionAsync(CancellationToken ct = default);
    Task RollbackTransactionAsync(CancellationToken ct = default);
}

// ═══════════════════════════════════════════════════════════
//  SERVEIS
// ═══════════════════════════════════════════════════════════

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
}

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default);
}

public interface IEmailService
{
    Task SendWelcomeEmailAsync(string toEmail, string userName, string password, string appUrl, CancellationToken ct = default);
    Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetToken, string appUrl, CancellationToken ct = default);
    Task SendAttendanceReminderAsync(string toEmail, string userName, string appUrl, CancellationToken ct = default);
    Task SendGenericEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default);
}

public interface IExcelImportService
{
    Task<ExcelImportResult<Student>> ImportStudentsFromEsferaAsync(
        Stream fileStream, Guid groupId, CancellationToken ct = default);
    Task<ExcelImportResult<Schedule>> ImportScheduleAsync(
        Stream fileStream, Guid academicYearId, CancellationToken ct = default);
}

public interface IExcelExportService
{
    Task<byte[]> ExportAttendanceReportAsync(AttendanceReportFilter filter, CancellationToken ct = default);
}

public interface IOllamaService
{
    Task<string> QueryAsync(string naturalLanguageQuery, string dataContext, CancellationToken ct = default);
    Task<string> ParsearCalendariPdfAsync(string textPdf, CancellationToken ct = default);
    Task<bool> IsAvailableAsync(CancellationToken ct = default);
}

public interface IAttendanceService
{
    Task<AttendanceSession> OpenSessionAsync(Guid scheduleId, DateOnly date, Guid userId, CancellationToken ct = default);
    Task CloseSessionAsync(Guid sessionId, Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<AttendanceSession>> GetTodaySessionsForTeacherAsync(Guid userId, CancellationToken ct = default);
    Task<AttendanceSummary> GetSummaryAsync(AttendanceReportFilter filter, CancellationToken ct = default);
}

// ═══════════════════════════════════════════════════════════
//  VALUE OBJECTS / RESULTS
// ═══════════════════════════════════════════════════════════

public record ExcelImportResult<T>
{
    public List<T> Imported { get; init; } = [];
    public List<ExcelImportError> Errors { get; init; } = [];
    public int TotalRows { get; init; }
    public int SuccessCount => Imported.Count;
    public int ErrorCount => Errors.Count;
}

public record ExcelImportError(int Row, string Column, string Message);

public record AttendanceReportFilter
{
    public Guid AcademicYearId { get; init; }
    public Guid? CycleId { get; init; }
    public Guid? GroupId { get; init; }
    public Guid? StudentId { get; init; }
    public Guid? TeacherId { get; init; }
    public Guid? SubjectId { get; init; }
    public DateOnly? FromDate { get; init; }
    public DateOnly? ToDate { get; init; }
    public bool? OnlyAbsences { get; init; }
}

public record AttendanceSummary
{
    public int TotalSessions { get; init; }
    public int TotalPresent { get; init; }
    public int TotalAbsent { get; init; }
    public int TotalJustified { get; init; }
    public decimal AttendancePercentage => TotalSessions > 0
        ? Math.Round((decimal)TotalPresent / TotalSessions * 100, 2)
        : 0;
}
