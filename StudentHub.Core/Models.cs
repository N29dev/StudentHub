using System.Collections.Immutable;

namespace StudentHub.Core;

public record Course(Guid Id, string Name, int Credits, decimal? Grade);
public record Assignment(Guid Id, Guid CourseId, string Title, DateOnly Due,
    int Importance, bool Completed = false);
public record Lesson(Guid Id, Guid CourseId, DayOfWeek Day, TimeOnly Start,
    TimeOnly End, string Room);

public record HubData(ImmutableArray<Course> Courses, ImmutableArray<Assignment> Assignments,
    ImmutableArray<Lesson> Lessons)
{
    public static HubData Empty => new([], [], []);
}

public record CourseProgress(Course Course, int Done, int Total);
public enum TaskStatus { Done, Overdue, Today, Upcoming }
