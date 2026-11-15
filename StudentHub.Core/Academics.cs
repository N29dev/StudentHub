namespace StudentHub.Core;

public static class Academics
{
    // Example 4.0 grading scale; change thresholds if your university uses another.
    public static decimal GradePoints(decimal grade) => grade switch
    {
        >= 95 => 4.00m, >= 90 => 3.67m, >= 85 => 3.33m,
        >= 80 => 3.00m, >= 75 => 2.67m, >= 70 => 2.33m,
        >= 65 => 2.00m, >= 60 => 1.67m, >= 55 => 1.33m,
        >= 50 => 1.00m, _ => 0m
    };

    public static decimal? Gpa(this IEnumerable<Course> courses)
    {
        var graded = courses.Where(c => c.Grade.HasValue).ToArray();
        var credits = graded.Sum(c => c.Credits);
        return credits == 0 ? null :
            decimal.Round(graded.Sum(c => GradePoints(c.Grade!.Value) * c.Credits) / credits, 2);
    }

    public static TaskStatus Status(this Assignment a, DateOnly today) => a switch
    {
        { Completed: true } => TaskStatus.Done,
        _ when a.Due < today => TaskStatus.Overdue,
        _ when a.Due == today => TaskStatus.Today,
        _ => TaskStatus.Upcoming
    };

    // Earlier deadlines and higher importance increase priority.
    public static int Priority(DateOnly today, Assignment a)
    {
        var days = a.Due.DayNumber - today.DayNumber;
        return a.Importance * 10 + (days switch
        {
            < 0 => 100, 0 => 80, 1 => 60, <= 3 => 40, <= 7 => 20, _ => 0
        });
    }

    public static IEnumerable<Assignment> Plan(this IEnumerable<Assignment> assignments, DateOnly today)
    {
        Func<DateOnly, Assignment, int> score = Priority;
        var rank = score.Curry()(today);
        return assignments.Where(a => !a.Completed)
            .OrderByDescending(rank).ThenBy(a => a.Due).ThenBy(a => a.Title);
    }

    public static IEnumerable<Assignment> Filter(this IEnumerable<Assignment> assignments,
        Func<Assignment, bool> predicate, string search)
    {
        Func<Assignment, string> title = a => a.Title;
        var normalized = title.Compose(s => s.ToUpperInvariant());
        var query = search.Trim().ToUpperInvariant();
        return assignments.Where(predicate).Where(a => normalized(a).Contains(query));
    }

    public static IEnumerable<CourseProgress> Progress(this HubData data)
        => data.Courses.Select(c =>
        {
            var tasks = data.Assignments.Where(a => a.CourseId == c.Id).ToArray();
            return new CourseProgress(c, tasks.Count(a => a.Completed), tasks.Length);
        });

    public static bool Overlaps(Lesson a, Lesson b)
        => a.Day == b.Day && a.Start < b.End && b.Start < a.End;

    public static string CourseName(this HubData data, Guid id)
        => data.Courses.FirstOrDefault(c => c.Id == id)?.Name ?? "Unknown course";
}
