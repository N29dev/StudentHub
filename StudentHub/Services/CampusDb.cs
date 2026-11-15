using Microsoft.EntityFrameworkCore;

namespace StudentHub.Services;

public static class Roles
{
    public const string Student = "Student";
    public const string Lecturer = "Lecturer";
}

public sealed class CampusAccount
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Name { get; set; } = "";
    public string Role { get; set; } = Roles.Student;
    public string Program { get; set; } = "";
    public string? Avatar { get; set; }
    public Guid? GroupId { get; set; }
}

public sealed class AcademicGroup
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public string Name { get; set; } = "";
    public string JoinCode { get; set; } = "";
}

public sealed class CourseOffering
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public string Name { get; set; } = "";
    public int Credits { get; set; }
}

public sealed class PublishedAssignment
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string Title { get; set; } = "";
    public DateOnly Due { get; set; }
    public int Importance { get; set; }
}

public sealed class ScheduledLesson
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public DayOfWeek Day { get; set; }
    public TimeOnly Start { get; set; }
    public TimeOnly End { get; set; }
    public string Room { get; set; } = "";
}

public sealed class CourseMark
{
    public Guid StudentId { get; set; }
    public Guid CourseId { get; set; }
    public decimal Grade { get; set; }
}

public sealed class Completion
{
    public Guid StudentId { get; set; }
    public Guid AssignmentId { get; set; }
}

public sealed class CampusDb(DbContextOptions<CampusDb> options) : DbContext(options)
{
    public DbSet<CampusAccount> Accounts => Set<CampusAccount>();
    public DbSet<AcademicGroup> Groups => Set<AcademicGroup>();
    public DbSet<CourseOffering> Courses => Set<CourseOffering>();
    public DbSet<PublishedAssignment> Assignments => Set<PublishedAssignment>();
    public DbSet<ScheduledLesson> Lessons => Set<ScheduledLesson>();
    public DbSet<CourseMark> Marks => Set<CourseMark>();
    public DbSet<Completion> Completions => Set<Completion>();

    protected override void OnModelCreating(ModelBuilder m)
    {
        m.Entity<CampusAccount>().HasIndex(x => x.Email).IsUnique();
        m.Entity<AcademicGroup>().HasIndex(x => x.JoinCode).IsUnique();
        m.Entity<AcademicGroup>().HasIndex(x => new { x.OwnerId, x.Name }).IsUnique();
        m.Entity<CourseOffering>().HasIndex(x => new { x.GroupId, x.Name }).IsUnique();
        m.Entity<CourseMark>().HasKey(x => new { x.StudentId, x.CourseId });
        m.Entity<Completion>().HasKey(x => new { x.StudentId, x.AssignmentId });

        m.Entity<AcademicGroup>().HasOne<CampusAccount>().WithMany().HasForeignKey(x => x.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);
        m.Entity<CampusAccount>().HasOne<AcademicGroup>().WithMany().HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Restrict);
        m.Entity<CourseOffering>().HasOne<AcademicGroup>().WithMany().HasForeignKey(x => x.GroupId);
        m.Entity<PublishedAssignment>().HasOne<CourseOffering>().WithMany().HasForeignKey(x => x.CourseId);
        m.Entity<ScheduledLesson>().HasOne<CourseOffering>().WithMany().HasForeignKey(x => x.CourseId);
        m.Entity<CourseMark>().HasOne<CourseOffering>().WithMany().HasForeignKey(x => x.CourseId);
        m.Entity<CourseMark>().HasOne<CampusAccount>().WithMany().HasForeignKey(x => x.StudentId);
        m.Entity<Completion>().HasOne<PublishedAssignment>().WithMany().HasForeignKey(x => x.AssignmentId);
        m.Entity<Completion>().HasOne<CampusAccount>().WithMany().HasForeignKey(x => x.StudentId);
    }
}
