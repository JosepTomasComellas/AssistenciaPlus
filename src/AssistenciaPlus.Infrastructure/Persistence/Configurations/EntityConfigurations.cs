using AssistenciaPlus.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistenciaPlus.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.HasKey(e => e.Id);
        b.HasIndex(e => e.Email).IsUnique();
        b.Property(e => e.Email).HasMaxLength(256).IsRequired();
        b.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
        b.Property(e => e.LastName).HasMaxLength(100).IsRequired();
        b.Property(e => e.PasswordHash).HasMaxLength(512).IsRequired();
        b.Property(e => e.Role).HasConversion<int>();
        b.Property(e => e.Language).HasMaxLength(2).HasDefaultValue("ca");
    }
}

public class AcademicYearConfiguration : IEntityTypeConfiguration<AcademicYear>
{
    public void Configure(EntityTypeBuilder<AcademicYear> b)
    {
        b.HasKey(e => e.Id);
        b.HasIndex(e => e.Name).IsUnique();
        b.Property(e => e.Name).HasMaxLength(20).IsRequired();
        // Garantia: màxim un any actiu
        b.HasIndex(e => e.IsActive).HasFilter("\"IsActive\" = true");
    }
}

public class CycleConfiguration : IEntityTypeConfiguration<Cycle>
{
    public void Configure(EntityTypeBuilder<Cycle> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.Code).HasMaxLength(20).IsRequired();
        b.Property(e => e.Name).HasMaxLength(150).IsRequired();
        b.HasOne(e => e.AcademicYear)
            .WithMany(a => a.Cycles)
            .HasForeignKey(e => e.AcademicYearId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.Code).HasMaxLength(10).IsRequired();
        b.Property(e => e.Name).HasMaxLength(100).IsRequired();
        b.HasOne(e => e.CourseYear)
            .WithMany(c => c.Groups)
            .HasForeignKey(e => e.CourseYearId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
        b.Property(e => e.LastName).HasMaxLength(100).IsRequired();
        b.Property(e => e.SecondLastName).HasMaxLength(100);
        b.Property(e => e.IdNumber).HasMaxLength(20);
        b.Property(e => e.Email).HasMaxLength(256);
        b.Ignore(e => e.FullName);
        b.HasOne(e => e.Group)
            .WithMany(g => g.Students)
            .HasForeignKey(e => e.GroupId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ScheduleConfiguration : IEntityTypeConfiguration<Schedule>
{
    public void Configure(EntityTypeBuilder<Schedule> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.DayOfWeek).HasConversion<int>();
        b.HasOne(e => e.Group)
            .WithMany(g => g.Schedules)
            .HasForeignKey(e => e.GroupId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(e => e.Teacher)
            .WithMany(u => u.Schedules)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(e => e.Subject)
            .WithMany(s => s.Schedules)
            .HasForeignKey(e => e.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AttendanceSessionConfiguration : IEntityTypeConfiguration<AttendanceSession>
{
    public void Configure(EntityTypeBuilder<AttendanceSession> b)
    {
        b.HasKey(e => e.Id);
        b.HasIndex(e => new { e.ScheduleId, e.Date }).IsUnique();
        b.Property(e => e.Status).HasConversion<int>();
        b.HasOne(e => e.Schedule)
            .WithMany(s => s.AttendanceSessions)
            .HasForeignKey(e => e.ScheduleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> b)
    {
        b.HasKey(e => e.Id);
        b.HasIndex(e => new { e.AttendanceSessionId, e.StudentId }).IsUnique();
        b.Property(e => e.Observation).HasMaxLength(500);
        b.Property(e => e.JustificationNote).HasMaxLength(500);
        b.HasOne(e => e.AttendanceSession)
            .WithMany(s => s.Records)
            .HasForeignKey(e => e.AttendanceSessionId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasOne(e => e.Student)
            .WithMany(s => s.AttendanceRecords)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(e => e.AttendanceStatus)
            .WithMany(s => s.Records)
            .HasForeignKey(e => e.AttendanceStatusId)
            .OnDelete(DeleteBehavior.Restrict);
        // Relació cap a User sense navegació inversa per evitar cicles
        b.HasOne(e => e.RecordedByUser)
            .WithMany(u => u.AttendanceRecords)
            .HasForeignKey(e => e.RecordedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AttendanceStatusConfiguration : IEntityTypeConfiguration<AttendanceStatus>
{
    public void Configure(EntityTypeBuilder<AttendanceStatus> b)
    {
        b.HasKey(e => e.Id);
        b.HasIndex(e => e.Code).IsUnique();
        b.Property(e => e.Code).HasMaxLength(5).IsRequired();
        b.Property(e => e.NameCa).HasMaxLength(50).IsRequired();
        b.Property(e => e.NameEs).HasMaxLength(50).IsRequired();
        b.Property(e => e.Color).HasMaxLength(10);
        b.Property(e => e.Icon).HasMaxLength(50);
    }
}
