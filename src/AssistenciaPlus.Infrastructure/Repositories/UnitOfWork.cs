using AssistenciaPlus.Core.Entities;
using AssistenciaPlus.Core.Interfaces;
using AssistenciaPlus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AssistenciaPlus.Infrastructure.Repositories;

// ═══════════════════════════════════════════════════════════
//  REPOSITORI GENÈRIC
// ═══════════════════════════════════════════════════════════

public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
        => await _dbSet.ToListAsync(ct);

    public async Task<T> AddAsync(T entity, CancellationToken ct = default)
    {
        await _dbSet.AddAsync(entity, ct);
        return entity;
    }

    public Task UpdateAsync(T entity, CancellationToken ct = default)
    {
        _context.Entry(entity).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(T entity, CancellationToken ct = default)
    {
        // Soft delete
        entity.IsDeleted = true;
        _context.Entry(entity).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => await _dbSet.AnyAsync(e => e.Id == id, ct);
}

// ═══════════════════════════════════════════════════════════
//  UNIT OF WORK
// ═══════════════════════════════════════════════════════════

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _transaction;

    public IRepository<AcademicYear> AcademicYears { get; }
    public IRepository<Cycle> Cycles { get; }
    public IRepository<CourseYear> CourseYears { get; }
    public IRepository<Group> Groups { get; }
    public IRepository<Student> Students { get; }
    public IRepository<User> Users { get; }
    public IRepository<Schedule> Schedules { get; }
    public IRepository<Subject> Subjects { get; }
    public IRepository<AttendanceSession> AttendanceSessions { get; }
    public IRepository<AttendanceRecord> AttendanceRecords { get; }
    public IRepository<AttendanceStatus> AttendanceStatuses { get; }
    public IRepository<HolidayDay> HolidayDays { get; }

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
        AcademicYears = new Repository<AcademicYear>(context);
        Cycles = new Repository<Cycle>(context);
        CourseYears = new Repository<CourseYear>(context);
        Groups = new Repository<Group>(context);
        Students = new Repository<Student>(context);
        Users = new Repository<User>(context);
        Schedules = new Repository<Schedule>(context);
        Subjects = new Repository<Subject>(context);
        AttendanceSessions = new Repository<AttendanceSession>(context);
        AttendanceRecords = new Repository<AttendanceRecord>(context);
        AttendanceStatuses = new Repository<AttendanceStatus>(context);
        HolidayDays = new Repository<HolidayDay>(context);
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);

    public async Task BeginTransactionAsync(CancellationToken ct = default)
        => _transaction = await _context.Database.BeginTransactionAsync(ct);

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction != null)
            await _transaction.CommitAsync(ct);
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction != null)
            await _transaction.RollbackAsync(ct);
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}

// ═══════════════════════════════════════════════════════════
//  SERVEI D'ASSISTÈNCIA
// ═══════════════════════════════════════════════════════════

public class AttendanceService : IAttendanceService
{
    private readonly IUnitOfWork _uow;
    private readonly ICacheService _cache;
    private readonly AppDbContext _context;

    public AttendanceService(IUnitOfWork uow, ICacheService cache, AppDbContext context)
    {
        _uow = uow; _cache = cache; _context = context;
    }

    public async Task<AttendanceSession> OpenSessionAsync(
        Guid scheduleId, DateOnly date, Guid userId, CancellationToken ct = default)
    {
        // Comprova si ja existeix
        var existing = await _context.AttendanceSessions
            .FirstOrDefaultAsync(s => s.ScheduleId == scheduleId && s.Date == date, ct);

        if (existing != null)
        {
            if (existing.Status == Core.Enums.AttendanceSessionStatus.Closed)
                throw new InvalidOperationException("Aquesta sessió ja ha estat tancada");
            return existing;
        }

        // Crea la sessió
        var session = new AttendanceSession
        {
            ScheduleId = scheduleId,
            Date = date,
            Status = Core.Enums.AttendanceSessionStatus.Open,
            OpenedAt = DateTime.UtcNow
        };
        await _uow.AttendanceSessions.AddAsync(session, ct);

        // Crea registres per a tots els alumnes del grup
        var schedule = await _context.Schedules
            .Include(s => s.Group)
            .ThenInclude(g => g.Students)
            .FirstOrDefaultAsync(s => s.Id == scheduleId, ct);

        if (schedule?.Group.Students != null)
        {
            var defaultStatus = await _context.AttendanceStatuses
                .FirstOrDefaultAsync(s => s.IsDefault && s.IsActive, ct);

            foreach (var student in schedule.Group.Students.Where(s => s.IsActive))
            {
                var record = new AttendanceRecord
                {
                    AttendanceSessionId = session.Id,
                    StudentId = student.Id,
                    AttendanceStatusId = defaultStatus?.Id ?? Guid.Parse("10000000-0000-0000-0000-000000000001"),
                    RecordedBy = userId,
                    RecordedAt = DateTime.UtcNow
                };
                await _uow.AttendanceRecords.AddAsync(record, ct);
            }
        }

        await _uow.SaveChangesAsync(ct);

        // Invalidar caché
        await _cache.RemoveByPrefixAsync($"attendance:session:{scheduleId}", ct);

        return session;
    }

    public async Task CloseSessionAsync(Guid sessionId, Guid userId, CancellationToken ct = default)
    {
        var session = await _uow.AttendanceSessions.GetByIdAsync(sessionId, ct)
            ?? throw new KeyNotFoundException("Sessió no trobada");

        session.Status = Core.Enums.AttendanceSessionStatus.Closed;
        session.ClosedAt = DateTime.UtcNow;
        session.ClosedBy = userId;

        await _uow.AttendanceSessions.UpdateAsync(session, ct);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<AttendanceSession>> GetTodaySessionsForTeacherAsync(
        Guid userId, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var dayOfWeek = DateTime.Today.DayOfWeek;

        var query = _context.Schedules
            .Include(s => s.Group).ThenInclude(g => g.CourseYear).ThenInclude(c => c.Cycle)
            .Include(s => s.Subject)
            .Include(s => s.Teacher)
            .Where(s => !s.IsDeleted &&
                        s.DayOfWeek == dayOfWeek &&
                        s.EffectiveFrom <= today &&
                        (s.EffectiveTo == null || s.EffectiveTo >= today));

        if (userId != Guid.Empty)
            query = query.Where(s => s.UserId == userId);

        var schedules = await query.ToListAsync(ct);

        var sessions = new List<AttendanceSession>();
        foreach (var schedule in schedules)
        {
            var session = await _context.AttendanceSessions
                .Include(s => s.Records).ThenInclude(r => r.Student)
                .Include(s => s.Records).ThenInclude(r => r.AttendanceStatus)
                .FirstOrDefaultAsync(s => s.ScheduleId == schedule.Id && s.Date == today, ct);

            if (session == null)
            {
                // Sessió pendent (no oberta encara)
                session = new AttendanceSession
                {
                    Id = Guid.Empty,
                    ScheduleId = schedule.Id,
                    Date = today,
                    Status = Core.Enums.AttendanceSessionStatus.Pending
                };
            }

            sessions.Add(session);
        }

        return sessions.OrderBy(s =>
            schedules.FirstOrDefault(sc => sc.Id == s.ScheduleId)?.StartTime).ToList();
    }

    public async Task<AttendanceSummary> GetSummaryAsync(
        AttendanceReportFilter filter, CancellationToken ct = default)
    {
        var query = _context.AttendanceRecords
            .Include(r => r.AttendanceStatus)
            .Include(r => r.AttendanceSession)
            .ThenInclude(s => s.Schedule)
            .Where(r => !r.IsDeleted);

        if (filter.GroupId.HasValue)
            query = query.Where(r => r.AttendanceSession.Schedule.GroupId == filter.GroupId);

        if (filter.StudentId.HasValue)
            query = query.Where(r => r.StudentId == filter.StudentId);

        if (filter.FromDate.HasValue)
            query = query.Where(r => r.AttendanceSession.Date >= filter.FromDate);

        if (filter.ToDate.HasValue)
            query = query.Where(r => r.AttendanceSession.Date <= filter.ToDate);

        var records = await query.ToListAsync(ct);

        return new AttendanceSummary
        {
            TotalSessions = records.Count,
            TotalPresent = records.Count(r => !r.AttendanceStatus.CountsAsAbsence),
            TotalAbsent = records.Count(r => r.AttendanceStatus.CountsAsAbsence && !r.IsJustified),
            TotalJustified = records.Count(r => r.IsJustified)
        };
    }
}
