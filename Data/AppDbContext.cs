using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;
using RaceDay.Models;

namespace RaceDay.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Each DbSet = one table
    public DbSet<role> Roles => Set<role>();
    public DbSet<user> Users => Set<user>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<EventEnrolment> EventEnrolments => Set<EventEnrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Email must be unique
        modelBuilder.Entity<user>().HasIndex(u => u.Email).IsUnique();

        // A participant can't enrol in the same category twice
        modelBuilder.Entity<EventEnrolment>()
            .HasIndex(e => new { e.ParticipantID, e.CategoryID }).IsUnique();

        // One result per enrolment
        modelBuilder.Entity<Result>().HasIndex(r => r.EnrolmentID).IsUnique();

        // Stops SQL Server from cascade-deleting users by accident
        modelBuilder.Entity<Event>()
            .HasOne(e => e.Organiser).WithMany().HasForeignKey(e => e.OrganiserID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EventEnrolment>()
            .HasOne(e => e.Participant).WithMany().HasForeignKey(e => e.ParticipantID)
            .OnDelete(DeleteBehavior.Restrict);

        // Pre-fill the two roles
        modelBuilder.Entity<role>().HasData(
            new role { RoleID = 1, RoleName = "Organiser" },
            new role { RoleID = 2, RoleName = "Participant" }
        );
    }
}