using AssistenciaPlus.Core.Entities;
using AssistenciaPlus.Core.Enums;
using AssistenciaPlus.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AssistenciaPlus.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    private readonly ICurrentUserService _currentUser;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserService currentUser)
        : base(options)
    {
        _currentUser = currentUser;
    }

    // ── DbSets ───────────────────────────────────────────────
    public DbSet<AcademicYear> AcademicYears => Set<AcademicYear>();
    public DbSet<Cycle> Cycles => Set<Cycle>();
    public DbSet<CourseYear> CourseYears => Set<CourseYear>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<StudentGroupHistory> StudentGroupHistories => Set<StudentGroupHistory>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Schedule> Schedules => Set<Schedule>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<AttendanceSession> AttendanceSessions => Set<AttendanceSession>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<AttendanceStatus> AttendanceStatuses => Set<AttendanceStatus>();
    public DbSet<HolidayDay> HolidayDays => Set<HolidayDay>();

    // ── Auditoria automàtica ─────────────────────────────────
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = userId;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = userId;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Aplicar totes les configuracions de la carpeta Configurations
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Filtre global de soft-delete
        builder.Entity<AcademicYear>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Cycle>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<CourseYear>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Group>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Student>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<User>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Schedule>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Subject>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<AttendanceSession>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<AttendanceRecord>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<AttendanceStatus>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<HolidayDay>().HasQueryFilter(e => !e.IsDeleted);

        // Seed: estats d'assistència per defecte
        SeedAttendanceStatuses(builder);
    }

    private static void SeedAttendanceStatuses(ModelBuilder builder)
    {
        builder.Entity<AttendanceStatus>().HasData(
            new AttendanceStatus
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                Code = "P",
                NameCa = "Present",
                NameEs = "Presente",
                Color = "#4CAF50",
                Icon = "check_circle",
                CountsAsAbsence = false,
                IsDefault = true,
                SortOrder = 1,
                IsActive = true
            },
            new AttendanceStatus
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
                Code = "A",
                NameCa = "Absent",
                NameEs = "Ausente",
                Color = "#F44336",
                Icon = "cancel",
                CountsAsAbsence = true,
                IsDefault = false,
                SortOrder = 2,
                IsActive = true
            },
            new AttendanceStatus
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000003"),
                Code = "R",
                NameCa = "Retard",
                NameEs = "Retraso",
                Color = "#FF9800",
                Icon = "schedule",
                CountsAsAbsence = false,
                IsDefault = false,
                SortOrder = 3,
                IsActive = true
            },
            new AttendanceStatus
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000004"),
                Code = "J",
                NameCa = "Justificat",
                NameEs = "Justificado",
                Color = "#2196F3",
                Icon = "assignment",
                CountsAsAbsence = false,
                IsDefault = false,
                SortOrder = 4,
                IsActive = true
            },
            new AttendanceStatus
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000005"),
                Code = "NP",
                NameCa = "No presentat",
                NameEs = "No presentado",
                Color = "#9C27B0",
                Icon = "help",
                CountsAsAbsence = true,
                IsDefault = false,
                SortOrder = 5,
                IsActive = true
            }
        );
    }
}
