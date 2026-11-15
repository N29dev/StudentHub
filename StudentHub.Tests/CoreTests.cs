using StudentHub.Core;
using StudentHub.Services;
using Xunit;

namespace StudentHub.Tests;

public class CoreTests
{
    private static readonly DateOnly Today = new(2026, 11, 15);
    private static readonly Course Math = new(Guid.NewGuid(), "Math", 5, 90);
    private static HubData Data => new([Math], [], []);
    private static Assignment Task(string name, int days, int importance = 2, bool done = false)
        => new(Guid.NewGuid(), Math.Id, name, Today.AddDays(days), importance, done);

    [Fact] public void GpaWeightedByCreditsAndIgnoresUngraded()
    {
        Course[] courses = [new(Guid.NewGuid(), "A", 5, 95), new(Guid.NewGuid(), "B", 3, 80),
            new(Guid.NewGuid(), "C", 10, null)];
        Assert.Equal(3.63m, courses.Gpa());
        Assert.Null(courses.Where(c => !c.Grade.HasValue).Gpa());
    }

    [Theory]
    [InlineData(100, 4.0)] [InlineData(95, 4.0)] [InlineData(90, 3.67)]
    [InlineData(50, 1.0)] [InlineData(49, 0.0)]
    public void GradingThresholds(int grade, double expected)
        => Assert.Equal((decimal)expected, Academics.GradePoints(grade));

    [Fact] public void SmartPlanOrdersByUrgencyAndExcludesCompleted()
    {
        Assignment[] tasks = [Task("Later", 10, 3), Task("Late", -1, 1), Task("Today", 0, 3), Task("Finished", -5, 3, true)];
        Assert.Equal(new[] { "Today", "Late", "Later" }.OrderBy(x => x),
            tasks.Plan(Today).Select(a => a.Title).OrderBy(x => x));
        Assert.Equal("Today", tasks.Plan(Today).First().Title);
    }

    [Fact] public void FilteringUsesPredicateAndIgnoresCase()
        => Assert.Single(new[] { Task("Math homework", 0), Task("Math test", 1, done: true) }
            .Filter(a => !a.Completed, "MATH"));

    [Fact] public void ResultShortCircuits()
    {
        Result<int> result = new Fail<int>("invalid");
        Assert.Equal("invalid", Assert.IsType<Fail<string>>(result.Map(n => n.ToString())
            .Bind(s => new Ok<string>(s))).Error);
        Assert.Equal(6, Assert.IsType<Ok<int>>(new Ok<int>(2).Map(x => x * 3)).Value);
    }

    [Fact] public void CurryingAndComposition()
    {
        Func<int, int, int> add = (a, b) => a + b;
        Func<int, int> twice = n => n * 2;
        Assert.Equal(14, add.Curry()(3).Compose(twice)(4));
    }

    [Fact] public void UpdateDoesNotMutateOriginal()
    {
        var a = Task("Homework", 1);
        var added = Assert.IsType<Ok<HubData>>(Changes.SaveAssignment(Data, a)).Value;
        var toggled = Assert.IsType<Ok<HubData>>(Changes.ToggleAssignment(added, a.Id)).Value;
        Assert.Empty(Data.Assignments);
        Assert.False(added.Assignments[0].Completed);
        Assert.True(toggled.Assignments[0].Completed);
        Assert.IsType<Fail<HubData>>(Changes.DeleteCourse(added, Math.Id));
    }

    [Theory] [InlineData(-1)] [InlineData(101)]
    public void InvalidGradeRejected(int grade)
        => Assert.IsType<Fail<HubData>>(Changes.SaveCourse(Data, Math with { Grade = grade }));

    [Fact] public void InvalidAssignmentRejected()
        => Assert.IsType<Fail<HubData>>(Changes.SaveAssignment(Data, Task(" ", 1)));

    [Fact] public void ScheduleRejectsOverlapButAllowsBackToBack()
    {
        var a = new Lesson(Guid.NewGuid(), Math.Id, DayOfWeek.Monday, new(9, 0), new(10, 0), "101");
        var added = Assert.IsType<Ok<HubData>>(Changes.SaveLesson(Data, a)).Value;
        var b = a with { Id = Guid.NewGuid(), Start = new(9, 30), End = new(10, 30) };
        Assert.IsType<Fail<HubData>>(Changes.SaveLesson(added, b));
        Assert.IsType<Ok<HubData>>(Changes.SaveLesson(added, b with { Start = new(10, 0) }));
    }

    [Fact] public void StorePersistsAndReloads()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "hub.json");
        try
        {
            var store = new HubStore(path);
            Assert.IsType<Ok<HubData>>(store.Change(_ => new Ok<HubData>(Data)));
            Assert.Equal(Math, Assert.Single(new HubStore(path).Data.Courses));
        }
        finally { if (Directory.Exists(Path.GetDirectoryName(path))) Directory.Delete(Path.GetDirectoryName(path)!, true); }
    }

    [Fact] public void CorruptDataIsNotOverwritten()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".json");
        try
        {
            File.WriteAllText(path, "{broken");
            var store = new HubStore(path);
            Assert.NotNull(store.LoadError);
            Assert.IsType<Fail<HubData>>(store.Change(_ => new Ok<HubData>(Data)));
            Assert.Equal("{broken", File.ReadAllText(path));
        }
        finally { File.Delete(path); }
    }
}
