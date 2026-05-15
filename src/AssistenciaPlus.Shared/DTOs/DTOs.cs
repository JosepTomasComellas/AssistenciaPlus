namespace AssistenciaPlus.Shared.DTOs;

// ── Auth ──────────────────────────────────────────────────

public record LoginRequestDto
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public record LoginResponseDto
{
    public string Token { get; init; } = string.Empty;
    public UserDto User { get; init; } = null!;
    public DateTime ExpiresAt { get; init; }
}

public record ChangePasswordDto
{
    public string CurrentPassword { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
}

public record ForgotPasswordDto
{
    public string Email { get; init; } = string.Empty;
}

public record ResetPasswordDto
{
    public string Token { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
}

// ── Usuaris ───────────────────────────────────────────────

public record UserDto
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string? PhotoPath { get; init; }
    public string Language { get; init; } = "ca";
    public DateTime? LastLogin { get; init; }
    public bool IsActive { get; init; } = true;
    public string FullName => $"{FirstName} {LastName}";
}

public record CreateUserDto
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = "Teacher";
    public string? Language { get; init; }
}

public record UpdateUserDto
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = "Teacher";
    public string? Language { get; init; }
    public bool IsActive { get; init; } = true;
}

// ── Assistència ───────────────────────────────────────────

public record AttendanceSessionDto
{
    public Guid Id { get; init; }
    public DateOnly Date { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public string? GroupCode { get; init; }
    public string? SubjectName { get; init; }
    public string? TeacherName { get; init; }
    public TimeOnly? StartTime { get; init; }
    public TimeOnly? EndTime { get; init; }
    public List<AttendanceRecordDto> Records { get; init; } = [];
}

public record AttendanceRecordDto
{
    public Guid Id { get; init; }
    public Guid StudentId { get; init; }
    public string StudentFullName { get; init; } = string.Empty;
    public Guid AttendanceStatusId { get; init; }
    public string StatusCode { get; init; } = string.Empty;
    public string StatusColor { get; init; } = string.Empty;
    public string? Observation { get; init; }
    public bool IsJustified { get; init; }
    public string? JustificationNote { get; init; }
}

public record UpdateAttendanceRecordDto
{
    public Guid AttendanceStatusId { get; init; }
    public string? Observation { get; init; }
    public bool IsJustified { get; init; }
    public string? JustificationNote { get; init; }
}

// ── Estructura acadèmica ───────────────────────────────────

public record AcademicYearDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public bool IsActive { get; init; }
}

public record GroupDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string CycleName { get; init; } = string.Empty;
    public int CourseYear { get; init; }
    public int StudentCount { get; init; }
}

public record StudentDto
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? SecondLastName { get; init; }
    public string? IdNumber { get; init; }
    public string? Email { get; init; }
    public bool IsActive { get; init; } = true;
    public string FullName => $"{LastName}{(SecondLastName != null ? " " + SecondLastName : "")}, {FirstName}";
}

// ── IA ────────────────────────────────────────────────────

public record NaturalLanguageQueryDto
{
    public string Query { get; init; } = string.Empty;
    public string? AcademicYearId { get; init; }
    public string? GroupId { get; init; }
}

public record NaturalLanguageResponseDto
{
    public string Answer { get; init; } = string.Empty;
    public bool IsAvailable { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
