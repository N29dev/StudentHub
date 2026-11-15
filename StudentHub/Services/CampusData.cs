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

public sealed class CampusData
{
    public List<CampusAccount> Accounts { get; set; } = [];
    public List<AcademicGroup> Groups { get; set; } = [];
    public List<CourseOffering> Courses { get; set; } = [];
    public List<PublishedAssignment> Assignments { get; set; } = [];
    public List<ScheduledLesson> Lessons { get; set; } = [];
    public List<CourseMark> Marks { get; set; } = [];
    public List<Completion> Completions { get; set; } = [];
}
