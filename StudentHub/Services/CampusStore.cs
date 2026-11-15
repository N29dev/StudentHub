using System.Text.Json;

namespace StudentHub.Services;

// A single server owns this file. Readers see the last successfully saved snapshot.
public sealed class CampusStore
{
    private readonly string path;
    private volatile CampusData data;
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public CampusStore(string path)
    {
        this.path = Path.GetFullPath(path);
        // Fail explicitly rather than silently overwriting a damaged file.
        data = File.Exists(this.path)
            ? JsonSerializer.Deserialize<CampusData>(File.ReadAllText(this.path), Options)
                ?? throw new JsonException("Invalid campus data.")
            : new CampusData();
        if (data.Accounts is null || data.Groups is null || data.Courses is null ||
            data.Assignments is null || data.Lessons is null || data.Marks is null || data.Completions is null)
            throw new JsonException("Invalid campus data.");
    }

    // Only CampusService reads the snapshot; it never changes it directly.
    internal CampusData Snapshot => data;

    internal CampusData Copy()
        => JsonSerializer.Deserialize<CampusData>(JsonSerializer.Serialize(data))!;

    internal void Save(CampusData next)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(next, Options));
        File.Move(temp, path, overwrite: true);
        data = next;
    }
}
