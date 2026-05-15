using AssistenciaPlus.Core.Enums;

namespace AssistenciaPlus.Core.Entities;

// ═══════════════════════════════════════════════════════════
//  ANY ACADÈMIC
// ═══════════════════════════════════════════════════════════

/// <summary>
/// Any acadèmic (p.ex. 2024-2025).
/// Permet múltiples anys amb historial complet.
/// Únicament un any pot estar actiu alhora.
/// </summary>
public class AcademicYear : BaseEntity
{
    public string Name { get; set; } = string.Empty;        // "2024-2025"
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsActive { get; set; } = false;

    public ICollection<Cycle> Cycles { get; set; } = [];
    public ICollection<Schedule> Schedules { get; set; } = [];
    public ICollection<HolidayDay> HolidayDays { get; set; } = [];
}

// ═══════════════════════════════════════════════════════════
//  ESTRUCTURA ACADÈMICA
// ═══════════════════════════════════════════════════════════

/// <summary>Cicle formatiu (p.ex. ASIX, DAM, SMX).</summary>
public class Cycle : BaseEntity
{
    public string Code { get; set; } = string.Empty;        // "ASIX"
    public string Name { get; set; } = string.Empty;        // "Administració de Sistemes"
    public string? Description { get; set; }
    public Guid AcademicYearId { get; set; }

    public AcademicYear AcademicYear { get; set; } = null!;
    public ICollection<CourseYear> CourseYears { get; set; } = [];
}

/// <summary>Curs dins d'un cicle (1r, 2n).</summary>
public class CourseYear : BaseEntity
{
    public int Year { get; set; }                           // 1 o 2
    public string Name { get; set; } = string.Empty;        // "Primer curs"
    public Guid CycleId { get; set; }

    public Cycle Cycle { get; set; } = null!;
    public ICollection<Group> Groups { get; set; } = [];
}

/// <summary>Grup d'alumnes (p.ex. 1A, 1B, 2A).</summary>
public class Group : BaseEntity
{
    public string Code { get; set; } = string.Empty;        // "1A"
    public string Name { get; set; } = string.Empty;        // "Primer A"
    public Guid CourseYearId { get; set; }

    public CourseYear CourseYear { get; set; } = null!;
    public ICollection<Student> Students { get; set; } = [];
    public ICollection<Schedule> Schedules { get; set; } = [];
}

// ═══════════════════════════════════════════════════════════
//  ALUMNES
// ═══════════════════════════════════════════════════════════

/// <summary>Alumne. Dades mínimes inicials, ampliables via Excel.</summary>
public class Student : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? SecondLastName { get; set; }
    public string? IdNumber { get; set; }                   // NIA o DNI
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;

    // Grup actual
    public Guid GroupId { get; set; }
    public Group Group { get; set; } = null!;

    // Historial de grups (canvis de grup durant el curs)
    public ICollection<StudentGroupHistory> GroupHistory { get; set; } = [];
    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = [];

    public string FullName => $"{LastName}{(SecondLastName != null ? " " + SecondLastName : "")}, {FirstName}";
}

/// <summary>Historial de canvis de grup d'un alumne.</summary>
public class StudentGroupHistory : BaseEntity
{
    public Guid StudentId { get; set; }
    public Guid GroupId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? Reason { get; set; }

    public Student Student { get; set; } = null!;
    public Group Group { get; set; } = null!;
}

// ═══════════════════════════════════════════════════════════
//  USUARIS
// ═══════════════════════════════════════════════════════════

/// <summary>Usuari del sistema amb rol assignat.</summary>
public class User : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Teacher;
    public string? PhotoPath { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLogin { get; set; }

    // Token per reset de contrasenya
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }

    // Preferències
    public string Language { get; set; } = "ca";            // "ca" | "es"

    public ICollection<Schedule> Schedules { get; set; } = [];
    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = [];

    public string FullName => $"{FirstName} {LastName}";
}

// ═══════════════════════════════════════════════════════════
//  HORARIS
// ═══════════════════════════════════════════════════════════

/// <summary>
/// Definició d'una sessió recurrent setmanal.
/// Un professor imparteix una matèria a un grup en un dia/hora.
/// </summary>
public class Schedule : BaseEntity
{
    public Guid AcademicYearId { get; set; }
    public Guid GroupId { get; set; }
    public Guid UserId { get; set; }                        // Professor
    public Guid SubjectId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int SessionNumber { get; set; }                  // Ordre dins del dia (1, 2, 3...)

    // Vigència (per si l'horari canvia a meitat de curs)
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }

    public AcademicYear AcademicYear { get; set; } = null!;
    public Group Group { get; set; } = null!;
    public User Teacher { get; set; } = null!;
    public Subject Subject { get; set; } = null!;
    public ICollection<AttendanceSession> AttendanceSessions { get; set; } = [];
}

/// <summary>Matèria o mòdul (p.ex. M01 - Sistemes Operatius).</summary>
public class Subject : BaseEntity
{
    public string Code { get; set; } = string.Empty;        // "M01"
    public string Name { get; set; } = string.Empty;        // "Sistemes Operatius"
    public Guid CycleId { get; set; }

    public Cycle Cycle { get; set; } = null!;
    public ICollection<Schedule> Schedules { get; set; } = [];
}

/// <summary>Dia festiu o no lectiu.</summary>
public class HolidayDay : BaseEntity
{
    public Guid AcademicYearId { get; set; }
    public DateOnly Date { get; set; }
    public string? Description { get; set; }                // "Festa local"

    public AcademicYear AcademicYear { get; set; } = null!;
}

// ═══════════════════════════════════════════════════════════
//  ASSISTÈNCIA
// ═══════════════════════════════════════════════════════════

/// <summary>
/// Sessió d'assistència: instància concreta d'un Schedule en una data.
/// Creada automàticament o quan el professor obre la llista.
/// </summary>
public class AttendanceSession : BaseEntity
{
    public Guid ScheduleId { get; set; }
    public DateOnly Date { get; set; }
    public AttendanceSessionStatus Status { get; set; } = AttendanceSessionStatus.Pending;
    public DateTime? OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public Guid? ClosedBy { get; set; }
    public string? Notes { get; set; }                      // Observació de la sessió

    public Schedule Schedule { get; set; } = null!;
    public ICollection<AttendanceRecord> Records { get; set; } = [];
}

/// <summary>
/// Registre d'assistència individual d'un alumne en una sessió.
/// </summary>
public class AttendanceRecord : BaseEntity
{
    public Guid AttendanceSessionId { get; set; }
    public Guid StudentId { get; set; }
    public Guid AttendanceStatusId { get; set; }
    public string? Observation { get; set; }                // Nota per a l'alumne
    public bool IsJustified { get; set; } = false;
    public string? JustificationNote { get; set; }
    public Guid? JustifiedBy { get; set; }
    public DateTime? JustifiedAt { get; set; }

    // Qui ha fet el registre
    public Guid RecordedBy { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    // Qui ha fet l'última modificació (equip directiu)
    public Guid? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }

    public AttendanceSession AttendanceSession { get; set; } = null!;
    public Student Student { get; set; } = null!;
    public AttendanceStatus AttendanceStatus { get; set; } = null!;
    public User RecordedByUser { get; set; } = null!;
}

/// <summary>
/// Estat d'assistència configurable (Present, Absent, Retard, Justificat...).
/// L'equip directiu pot definir els seus propis estats.
/// </summary>
public class AttendanceStatus : BaseEntity
{
    public string Code { get; set; } = string.Empty;        // "P", "A", "R", "J"
    public string NameCa { get; set; } = string.Empty;      // "Present" (català)
    public string NameEs { get; set; } = string.Empty;      // "Presente" (castellà)
    public string Color { get; set; } = "#4CAF50";          // Color per la UI
    public string Icon { get; set; } = "check_circle";      // Icona MudBlazor
    public bool CountsAsAbsence { get; set; } = false;
    public bool IsDefault { get; set; } = false;            // Estat per defecte al obrir llista
    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    public ICollection<AttendanceRecord> Records { get; set; } = [];
}
