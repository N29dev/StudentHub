using System.Collections.Immutable;

namespace StudentHub.Core;

// All operations return new immutable snapshots; none write to disk or read the clock.
public static class Changes
{
    private static Result<string> Text(string value, string label, int max = 100)
        => string.IsNullOrWhiteSpace(value) || value.Trim().Length > max
            ? new Fail<string>($"{label} must contain 1–{max} characters.")
            : new Ok<string>(value.Trim());

    private static Result<Course> ValidateCourse(Course c)
        => Text(c.Name, "Course name").Bind(name =>
            c.Credits is < 1 or > 30 ? new Fail<Course>("Credits must be between 1 and 30.") :
            c.Grade is < 0 or > 100 ? new Fail<Course>("Grade must be between 0 and 100.") :
            new Ok<Course>(c with { Name = name }));

    public static Result<HubData> SaveCourse(HubData data, Course course)
        => ValidateCourse(course).Bind(c =>
            data.Courses.Any(x => x.Id != c.Id && x.Name.Equals(c.Name, StringComparison.OrdinalIgnoreCase))
                ? new Fail<HubData>("A course with this name already exists.")
                : new Ok<HubData>(data with
                {
                    Courses = data.Courses.Any(x => x.Id == c.Id)
                        ? data.Courses.Select(x => x.Id == c.Id ? c : x).ToImmutableArray()
                        : data.Courses.Add(c)
                }));

    public static Result<HubData> DeleteCourse(HubData data, Guid id)
        => data.Assignments.Any(a => a.CourseId == id) || data.Lessons.Any(l => l.CourseId == id)
            ? new Fail<HubData>("Remove this course's assignments and lessons first.")
            : new Ok<HubData>(data with { Courses = data.Courses.RemoveAll(c => c.Id == id) });

    public static Result<HubData> SaveAssignment(HubData data, Assignment assignment)
        => Text(assignment.Title, "Assignment title").Bind(title =>
            !data.Courses.Any(c => c.Id == assignment.CourseId) ? new Fail<HubData>("Select a course.") :
            assignment.Importance is < 1 or > 3 ? new Fail<HubData>("Select a valid importance.") :
            assignment.Due == default ? new Fail<HubData>("Choose a due date.") :
            new Ok<HubData>(data with
            {
                Assignments = data.Assignments.Any(a => a.Id == assignment.Id)
                    ? data.Assignments.Select(a => a.Id == assignment.Id
                        ? assignment with { Title = title } : a).ToImmutableArray()
                    : data.Assignments.Add(assignment with { Title = title })
            }));

    public static Result<HubData> ToggleAssignment(HubData data, Guid id)
        => data.Assignments.FirstOrDefault(a => a.Id == id) is { } a
            ? SaveAssignment(data, a with { Completed = !a.Completed })
            : new Fail<HubData>("Assignment not found.");

    public static Result<HubData> DeleteAssignment(HubData data, Guid id)
        => new Ok<HubData>(data with { Assignments = data.Assignments.RemoveAll(a => a.Id == id) });

    public static Result<HubData> SaveLesson(HubData data, Lesson lesson)
        => Text(lesson.Room, "Room", 60).Bind(room =>
            !data.Courses.Any(c => c.Id == lesson.CourseId) ? new Fail<HubData>("Select a course.") :
            !Enum.IsDefined(lesson.Day) ? new Fail<HubData>("Select a valid day.") :
            lesson.Start >= lesson.End ? new Fail<HubData>("End time must be after start time.") :
            data.Lessons.Any(l => l.Id != lesson.Id && Academics.Overlaps(l, lesson))
                ? new Fail<HubData>("This time overlaps another lesson.")
                : new Ok<HubData>(data with
                {
                    Lessons = data.Lessons.Any(l => l.Id == lesson.Id)
                        ? data.Lessons.Select(l => l.Id == lesson.Id ? lesson with { Room = room } : l).ToImmutableArray()
                        : data.Lessons.Add(lesson with { Room = room })
                }));

    public static Result<HubData> DeleteLesson(HubData data, Guid id)
        => new Ok<HubData>(data with { Lessons = data.Lessons.RemoveAll(l => l.Id == id) });
}
