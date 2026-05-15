using AssistenciaPlus.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssistenciaPlus.Infrastructure.Data;

/// <summary>
/// Context principal de la base de dades PostgreSQL.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ── DbSets ──────────────────────────────────────────────
    public DbSet<AnyAcademic> AnysAcademics { get; set; }
    public DbSet<DiaCalendari> DiesCalendari { get; set; }
    public DbSet<Cicle> Cicles { get; set; }
    public DbSet<Curs> Cursos { get; set; }
    public DbSet<Grup> Grups { get; set; }
    public DbSet<Alumne> Alumnes { get; set; }
    public DbSet<FranjaHoraria> FranjesHoraries { get; set; }
    public DbSet<RegistreAssistencia> RegistresAssistencia { get; set; }
    public DbSet<AssistenciaAlumne> AssistenciesAlumnes { get; set; }
    public DbSet<Usuari> Usuaris { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Soft delete global filter ──────────────────────
        modelBuilder.Entity<Alumne>().HasQueryFilter(a => !a.IsDeleted);
        modelBuilder.Entity<Usuari>().HasQueryFilter(u => !u.IsDeleted);
        modelBuilder.Entity<Grup>().HasQueryFilter(g => !g.IsDeleted);

        // ── AnyAcademic ────────────────────────────────────
        modelBuilder.Entity<AnyAcademic>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Nom).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.EsActiu);
        });

        // ── DiaCalendari ───────────────────────────────────
        modelBuilder.Entity<DiaCalendari>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.AnyAcademicId, x.Data }).IsUnique();
            e.Property(x => x.TipusDia).HasConversion<int>();
        });

        // ── Cicle ──────────────────────────────────────────
        modelBuilder.Entity<Cicle>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Nom).HasMaxLength(60).IsRequired();
        });

        // ── Curs ───────────────────────────────────────────
        modelBuilder.Entity<Curs>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Nom).HasMaxLength(60).IsRequired();
            e.Property(x => x.Codi).HasMaxLength(10).IsRequired();
            e.HasOne(x => x.Cicle)
             .WithMany(x => x.Cursos)
             .HasForeignKey(x => x.CicleId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Grup ───────────────────────────────────────────
        modelBuilder.Entity<Grup>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Lletra).HasMaxLength(2);
            e.Ignore(x => x.NomComplet); // Propietat calculada
            e.HasOne(x => x.Curs)
             .WithMany(x => x.Grups)
             .HasForeignKey(x => x.CursId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.AnyAcademic)
             .WithMany(x => x.Grups)
             .HasForeignKey(x => x.AnyAcademicId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Tutor)
             .WithMany(x => x.GrupsTutoritzats)
             .HasForeignKey(x => x.TutorId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => new { x.CursId, x.AnyAcademicId, x.Lletra }).IsUnique();
        });

        // ── Alumne ─────────────────────────────────────────
        modelBuilder.Entity<Alumne>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Nom).HasMaxLength(60).IsRequired();
            e.Property(x => x.Cognom1).HasMaxLength(60).IsRequired();
            e.Property(x => x.Cognom2).HasMaxLength(60);
            e.Property(x => x.EmailFamilia).HasMaxLength(120);
            e.Ignore(x => x.NomComplet);
            e.Ignore(x => x.NomFusteta);
            e.HasOne(x => x.Grup)
             .WithMany(x => x.Alumnes)
             .HasForeignKey(x => x.GrupId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── FranjaHoraria ──────────────────────────────────
        modelBuilder.Entity<FranjaHoraria>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Nom).HasMaxLength(40).IsRequired();
            e.Ignore(x => x.DuradaMinuts);
        });

        // ── RegistreAssistencia ────────────────────────────
        modelBuilder.Entity<RegistreAssistencia>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.GrupId, x.FranjaHorariaId, x.Data }).IsUnique();
            e.HasOne(x => x.Grup)
             .WithMany(x => x.RegistresAssistencia)
             .HasForeignKey(x => x.GrupId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.FranjaHoraria)
             .WithMany(x => x.RegistresAssistencia)
             .HasForeignKey(x => x.FranjaHorariaId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Mestre)
             .WithMany(x => x.RegistresPassats)
             .HasForeignKey(x => x.MestreId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── AssistenciaAlumne ──────────────────────────────
        modelBuilder.Entity<AssistenciaAlumne>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.RegistreAssistenciaId, x.AlumneId }).IsUnique();
            e.Property(x => x.Estat).HasConversion<int>();
            e.Property(x => x.Motiu).HasConversion<int?>();
            e.HasOne(x => x.RegistreAssistencia)
             .WithMany(x => x.Assistencies)
             .HasForeignKey(x => x.RegistreAssistenciaId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Alumne)
             .WithMany(x => x.Assistencies)
             .HasForeignKey(x => x.AlumneId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Usuari ─────────────────────────────────────────
        modelBuilder.Entity<Usuari>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).HasMaxLength(120).IsRequired();
            e.Property(x => x.Nom).HasMaxLength(60).IsRequired();
            e.Property(x => x.Cognom1).HasMaxLength(60).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(256).IsRequired();
            e.Property(x => x.Rol).HasConversion<int>();
            e.Property(x => x.Idioma).HasMaxLength(2).HasDefaultValue("ca");
            e.Ignore(x => x.NomComplet);
        });
    }

    /// <summary>
    /// Actualitza automàticament UpdatedAt en cada Save.
    /// </summary>
    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State == EntityState.Modified);
        foreach (var entry in entries)
            entry.Entity.UpdatedAt = DateTime.UtcNow;
    }
}
