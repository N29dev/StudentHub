using System.Text.Json;
using StudentHub.Core;

namespace StudentHub.Services;

// Local, single-profile storage. A lock serializes changes across browser connections.
public sealed class HubStore
{
    private readonly string path;
    private readonly object gate = new();
    private HubData data;
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    public string? LoadError { get; }

    public HubStore(string path)
    {
        this.path = path;
        try
        {
            data = File.Exists(path)
                ? JsonSerializer.Deserialize<HubData>(File.ReadAllText(path), Options)
                    ?? throw new JsonException("Missing data.")
                : Sample(DateOnly.FromDateTime(DateTime.Today));
            if (data.Courses.IsDefault || data.Assignments.IsDefault || data.Lessons.IsDefault)
                throw new JsonException("Invalid data.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            data = HubData.Empty;
            LoadError = "Could not read saved data. Back up App_Data and fix the file before saving.";
        }
    }

    public HubData Data { get { lock (gate) return data; } }

    public Result<HubData> Change(Func<HubData, Result<HubData>> change)
    {
        lock (gate)
        {
            if (LoadError is not null) return new Fail<HubData>(LoadError);
            var result = change(data);
            if (result is not Ok<HubData> ok) return result;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
                File.WriteAllText(path + ".tmp", JsonSerializer.Serialize(ok.Value, Options));
                File.Move(path + ".tmp", path, overwrite: true);
                data = ok.Value;
                return result;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return new Fail<HubData>("Could not save changes. Check storage permissions.");
            }
        }
    }

    public static HubData Sample(DateOnly today)
    {
        var fp = new Course(Guid.NewGuid(), "Functional Programming", 5, 92);
        var math = new Course(Guid.NewGuid(), "Discrete Mathematics", 5, 88);
        var english = new Course(Guid.NewGuid(), "English", 3, null);
        return new HubData([fp, math, english],
        [
            new(Guid.NewGuid(), fp.Id, "Final project", today.AddDays(1), 3),
            new(Guid.NewGuid(), math.Id, "Graph theory exercises", today.AddDays(3), 2),
            new(Guid.NewGuid(), english.Id, "Vocabulary review", today.AddDays(-1), 1),
            new(Guid.NewGuid(), fp.Id, "LINQ practice", today.AddDays(-2), 2, true)
        ],
        [
            new(Guid.NewGuid(), fp.Id, DayOfWeek.Monday, new(10, 0), new(11, 50), "B2.1"),
            new(Guid.NewGuid(), math.Id, DayOfWeek.Wednesday, new(9, 0), new(10, 50), "C1.3"),
            new(Guid.NewGuid(), english.Id, DayOfWeek.Friday, new(12, 0), new(13, 0), "A2.4")
        ]);
    }
}
