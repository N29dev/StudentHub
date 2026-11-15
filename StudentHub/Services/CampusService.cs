using System.Collections.Immutable;
using System.Net.Mail;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StudentHub.Core;

namespace StudentHub.Services;

public record ProfileInfo(Guid Id, string Email, string Name, string Role, string Program,
    string? Avatar, Guid? GroupId, string? GroupName)
{
    public string Initials => string.Concat(Name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Take(2).Select(s => char.ToUpperInvariant(s[0])));
}
public record GroupInfo(Guid Id, string Name, string JoinCode);
public record StudentSummary(Guid Id, string Name, string Email, int Completed, int Total, decimal? Gpa);
public record GradeEntry(Guid StudentId, Guid CourseId, decimal Grade);
public record LecturerView(HubData Data, StudentSummary[] Students, GradeEntry[] Grades)
{
    public static LecturerView Empty => new(HubData.Empty, [], []);
}

// Database side effects live here; the existing core performs academic validation and calculations.
public sealed class CampusService(IDbContextFactory<CampusDb> factory, IConfiguration config)
{
    private readonly object gate = new();
    private readonly PasswordHasher<CampusAccount> hasher = new();

    private static Result<T> Fail<T>(string error) => new Fail<T>(error);
    private static Result<bool> Yes() => new Ok<bool>(true);
    private static bool ValidText(string s, int max) => !string.IsNullOrWhiteSpace(s) && s.Trim().Length <= max;

    private Result<T> Write<T>(Func<CampusDb, Result<T>> operation)
    {
        lock (gate)
        {
            using var db = factory.CreateDbContext();
            try
            {
                var result = operation(db);
                if (result is Ok<T>) db.SaveChanges();
                return result;
            }
            catch (Exception e) when (e is DbUpdateException or SqliteException)
            {
                return Fail<T>("Unable to save changes. Check the database and try again.");
            }
        }
    }

    private static ProfileInfo Profile(CampusDb db, CampusAccount a)
        => new(a.Id, a.Email, a.Name, a.Role, a.Program, a.Avatar, a.GroupId,
            a.GroupId is Guid id ? db.Groups.Find(id)?.Name : null);

    public ProfileInfo? GetProfile(Guid id)
    {
        using var db = factory.CreateDbContext();
        return db.Accounts.Find(id) is { } a ? Profile(db, a) : null;
    }

    public Result<ProfileInfo> Register(string name, string email, string password, string role,
        string lecturerCode, string groupCode)
    {
        email = email.Trim().ToLowerInvariant();
        name = name.Trim();
        if (!ValidText(name, 100)) return Fail<ProfileInfo>("Enter your full name (1–100 characters).");
        if (email.Length > 200 || !MailAddress.TryCreate(email, out var address) || address.Address != email)
            return Fail<ProfileInfo>("Enter a valid email address.");
        if (password.Length is < 8 or > 128) return Fail<ProfileInfo>("Password must contain 8–128 characters.");
        if (role is not (Roles.Student or Roles.Lecturer)) return Fail<ProfileInfo>("Invalid role.");
        if (role == Roles.Lecturer && (string.IsNullOrEmpty(config["StudentHub:LecturerCode"]) ||
            !string.Equals(lecturerCode, config["StudentHub:LecturerCode"], StringComparison.Ordinal)))
            return Fail<ProfileInfo>("A valid lecturer invitation code is required.");

        return Write<ProfileInfo>(db =>
        {
            if (db.Accounts.Any(a => a.Email == email)) return Fail<ProfileInfo>("This email is already registered.");
            var account = new CampusAccount { Id = Guid.NewGuid(), Name = name, Email = email, Role = role };
            if (role == Roles.Student && !string.IsNullOrWhiteSpace(groupCode))
            {
                var group = db.Groups.FirstOrDefault(g => g.JoinCode == groupCode.Trim().ToUpperInvariant());
                if (group is null) return Fail<ProfileInfo>("Invalid group code.");
                account.GroupId = group.Id;
            }
            account.PasswordHash = hasher.HashPassword(account, password);
            db.Accounts.Add(account);
            return new Ok<ProfileInfo>(Profile(db, account));
        });
    }

    public Result<ProfileInfo> Login(string email, string password)
    {
        if (password.Length > 128 || email.Length > 200) return Fail<ProfileInfo>("Incorrect email or password.");
        using var db = factory.CreateDbContext();
        var a = db.Accounts.FirstOrDefault(x => x.Email == email.Trim().ToLowerInvariant());
        if (a is null || hasher.VerifyHashedPassword(a, a.PasswordHash, password) == PasswordVerificationResult.Failed)
            return Fail<ProfileInfo>("Incorrect email or password.");
        return new Ok<ProfileInfo>(Profile(db, a));
    }

    public Result<bool> UpdateProfile(Guid user, string name, string program)
        => Write<bool>(db =>
        {
            var a = db.Accounts.Find(user);
            if (a is null) return Fail<bool>("Account not found.");
            if (!ValidText(name, 100) || program.Trim().Length > 100)
                return Fail<bool>("Check your name and program length.");
            a.Name = name.Trim(); a.Program = program.Trim();
            return Yes();
        });

    public Result<bool> SetAvatar(Guid user, byte[]? bytes)
        => Write<bool>(db =>
        {
            var a = db.Accounts.Find(user);
            if (a is null) return Fail<bool>("Account not found.");
            if (bytes is null) { a.Avatar = null; return Yes(); }
            if (bytes.Length > 512 * 1024) return Fail<bool>("Image must be smaller than 512 KB.");
            var png = bytes.AsSpan().StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
            var jpg = bytes.AsSpan().StartsWith(new byte[] { 255, 216, 255 });
            if (!png && !jpg) return Fail<bool>("Use a PNG or JPEG image.");
            a.Avatar = $"data:image/{(png ? "png" : "jpeg")};base64,{Convert.ToBase64String(bytes)}";
            return Yes();
        });

    public Result<bool> JoinGroup(Guid user, string code)
        => Write<bool>(db =>
        {
            var a = db.Accounts.Find(user);
            if (a?.Role != Roles.Student) return Fail<bool>("Only students can join groups.");
            if (a.GroupId.HasValue) return Fail<bool>("You already belong to a group.");
            var group = db.Groups.FirstOrDefault(g => g.JoinCode == code.Trim().ToUpperInvariant());
            if (group is null) return Fail<bool>("Invalid group code.");
            a.GroupId = group.Id;
            return Yes();
        });

    public GroupInfo[] MyGroups(Guid user)
    {
        using var db = factory.CreateDbContext();
        if (db.Accounts.Find(user)?.Role != Roles.Lecturer) return [];
        return db.Groups.Where(g => g.OwnerId == user).OrderBy(g => g.Name)
            .Select(g => new GroupInfo(g.Id, g.Name, g.JoinCode)).ToArray();
    }

    private static bool Owns(CampusDb db, Guid user, Guid group)
        => db.Accounts.Find(user)?.Role == Roles.Lecturer &&
            db.Groups.Any(g => g.Id == group && g.OwnerId == user);

    public Result<Guid> CreateGroup(Guid user, string name)
        => Write<Guid>(db =>
        {
            if (db.Accounts.Find(user)?.Role != Roles.Lecturer) return Fail<Guid>("Lecturer access required.");
            if (!ValidText(name, 40)) return Fail<Guid>("Enter a group name.");
            name = name.Trim().ToUpperInvariant();
            if (db.Groups.Any(g => g.OwnerId == user && g.Name == name)) return Fail<Guid>("Group already exists.");
            var group = new AcademicGroup { Id = Guid.NewGuid(), OwnerId = user, Name = name,
                JoinCode = Convert.ToHexString(RandomNumberGenerator.GetBytes(5)) };
            db.Groups.Add(group);
            return new Ok<Guid>(group.Id);
        });

    private static HubData GroupData(CampusDb db, Guid group, Guid? student = null)
    {
        var courses = db.Courses.Where(c => c.GroupId == group).ToArray();
        var ids = courses.Select(c => c.Id).ToArray();
        var marks = student.HasValue ? db.Marks.Where(m => m.StudentId == student.Value && ids.Contains(m.CourseId))
            .ToDictionary(m => m.CourseId, m => m.Grade) : new Dictionary<Guid, decimal>();
        var tasks = db.Assignments.Where(a => ids.Contains(a.CourseId)).ToArray();
        var taskIds = tasks.Select(a => a.Id).ToArray();
        var completed = student.HasValue ? db.Completions.Where(c => c.StudentId == student.Value &&
            taskIds.Contains(c.AssignmentId)).Select(c => c.AssignmentId).ToHashSet() : new HashSet<Guid>();
        var lessons = db.Lessons.Where(l => ids.Contains(l.CourseId)).ToArray();
        return new(
            courses.Select(c => new Course(c.Id, c.Name, c.Credits,
                marks.TryGetValue(c.Id, out var grade) ? grade : null)).ToImmutableArray(),
            tasks.Select(a => new Assignment(a.Id, a.CourseId, a.Title, a.Due, a.Importance,
                completed.Contains(a.Id))).ToImmutableArray(),
            lessons.Select(l => new Lesson(l.Id, l.CourseId, l.Day, l.Start, l.End, l.Room)).ToImmutableArray());
    }

    public HubData StudentData(Guid user)
    {
        using var db = factory.CreateDbContext();
        var a = db.Accounts.Find(user);
        return a?.Role == Roles.Student && a.GroupId is Guid group ? GroupData(db, group, user) : HubData.Empty;
    }

    public LecturerView ViewGroup(Guid user, Guid group)
    {
        using var db = factory.CreateDbContext();
        if (!Owns(db, user, group)) return LecturerView.Empty;
        var data = GroupData(db, group);
        var students = db.Accounts.Where(a => a.GroupId == group && a.Role == Roles.Student)
            .Select(a => new { a.Id, a.Name, a.Email }).OrderBy(a => a.Name).ToArray();
        var ids = students.Select(s => s.Id).ToArray();
        var courseIds = data.Courses.Select(c => c.Id).ToArray();
        var taskIds = data.Assignments.Select(a => a.Id).ToArray();
        var grades = db.Marks.Where(m => ids.Contains(m.StudentId) && courseIds.Contains(m.CourseId))
            .Select(m => new GradeEntry(m.StudentId, m.CourseId, m.Grade)).ToArray();
        var counts = db.Completions.Where(c => ids.Contains(c.StudentId) && taskIds.Contains(c.AssignmentId))
            .ToArray().GroupBy(c => c.StudentId).ToDictionary(g => g.Key, g => g.Count());
        var summaries = students.Select(s =>
        {
            var ownGrades = grades.Where(g => g.StudentId == s.Id).ToDictionary(g => g.CourseId, g => g.Grade);
            var courses = data.Courses.Select(c => c with
            { Grade = ownGrades.TryGetValue(c.Id, out var g) ? g : null });
            return new StudentSummary(s.Id, s.Name, s.Email, counts.GetValueOrDefault(s.Id),
                data.Assignments.Length, courses.Gpa());
        }).ToArray();
        return new(data, summaries, grades);
    }

    public Result<bool> SaveCourse(Guid user, Guid group, Course course)
        => Write<bool>(db =>
        {
            if (!Owns(db, user, group)) return Fail<bool>("Access denied.");
            var existing = db.Courses.Find(course.Id);
            if (existing is not null && existing.GroupId != group) return Fail<bool>("Access denied.");
            var result = Changes.SaveCourse(GroupData(db, group), course with { Grade = null });
            if (result is Fail<HubData> fail) return Fail<bool>(fail.Error);
            if (existing is null) db.Courses.Add(new CourseOffering
            { Id = course.Id, GroupId = group, Name = course.Name.Trim(), Credits = course.Credits });
            else { existing.Name = course.Name.Trim(); existing.Credits = course.Credits; }
            return Yes();
        });

    public Result<bool> DeleteCourse(Guid user, Guid group, Guid id)
        => Write<bool>(db =>
        {
            if (!Owns(db, user, group)) return Fail<bool>("Access denied.");
            var c = db.Courses.Find(id);
            if (c?.GroupId != group) return Fail<bool>("Course not found.");
            var result = Changes.DeleteCourse(GroupData(db, group), id);
            if (result is Fail<HubData> fail) return Fail<bool>(fail.Error);
            if (db.Marks.Any(m => m.CourseId == id)) return Fail<bool>("Remove the course grades first.");
            db.Courses.Remove(c); return Yes();
        });

    public Result<bool> SaveAssignment(Guid user, Guid group, Assignment a)
        => Write<bool>(db =>
        {
            if (!Owns(db, user, group)) return Fail<bool>("Access denied.");
            var existing = db.Assignments.Find(a.Id);
            if (existing is not null && !db.Courses.Any(c => c.Id == existing.CourseId && c.GroupId == group))
                return Fail<bool>("Access denied.");
            var result = Changes.SaveAssignment(GroupData(db, group), a);
            if (result is Fail<HubData> fail) return Fail<bool>(fail.Error);
            if (existing is null) db.Assignments.Add(new PublishedAssignment
            { Id = a.Id, CourseId = a.CourseId, Title = a.Title.Trim(), Due = a.Due, Importance = a.Importance });
            else { existing.CourseId = a.CourseId; existing.Title = a.Title.Trim(); existing.Due = a.Due; existing.Importance = a.Importance; }
            return Yes();
        });

    public Result<bool> DeleteAssignment(Guid user, Guid group, Guid id)
        => Write<bool>(db =>
        {
            if (!Owns(db, user, group)) return Fail<bool>("Access denied.");
            var a = db.Assignments.Find(id);
            if (a is null || !db.Courses.Any(c => c.Id == a.CourseId && c.GroupId == group))
                return Fail<bool>("Assignment not found.");
            db.Assignments.Remove(a); return Yes();
        });

    public Result<bool> Toggle(Guid user, Guid id)
        => Write<bool>(db =>
        {
            var student = db.Accounts.Find(user);
            var a = db.Assignments.Find(id);
            if (student?.Role != Roles.Student || student.GroupId is not Guid group || a is null ||
                !db.Courses.Any(c => c.Id == a.CourseId && c.GroupId == group))
                return Fail<bool>("Assignment not found.");
            var item = db.Completions.Find(user, id);
            if (item is null) db.Completions.Add(new Completion { StudentId = user, AssignmentId = id });
            else db.Completions.Remove(item);
            return Yes();
        });

    public Result<bool> SaveLesson(Guid user, Guid group, Lesson l)
        => Write<bool>(db =>
        {
            if (!Owns(db, user, group)) return Fail<bool>("Access denied.");
            var existing = db.Lessons.Find(l.Id);
            if (existing is not null && !db.Courses.Any(c => c.Id == existing.CourseId && c.GroupId == group))
                return Fail<bool>("Access denied.");
            var result = Changes.SaveLesson(GroupData(db, group), l);
            if (result is Fail<HubData> fail) return Fail<bool>(fail.Error);
            if (existing is null) db.Lessons.Add(new ScheduledLesson
            { Id = l.Id, CourseId = l.CourseId, Day = l.Day, Start = l.Start, End = l.End, Room = l.Room.Trim() });
            else { existing.CourseId = l.CourseId; existing.Day = l.Day; existing.Start = l.Start; existing.End = l.End; existing.Room = l.Room.Trim(); }
            return Yes();
        });

    public Result<bool> DeleteLesson(Guid user, Guid group, Guid id)
        => Write<bool>(db =>
        {
            if (!Owns(db, user, group)) return Fail<bool>("Access denied.");
            var l = db.Lessons.Find(id);
            if (l is null || !db.Courses.Any(c => c.Id == l.CourseId && c.GroupId == group))
                return Fail<bool>("Lesson not found.");
            db.Lessons.Remove(l); return Yes();
        });

    public Result<bool> SetGrade(Guid user, Guid group, Guid student, Guid course, decimal? grade)
        => Write<bool>(db =>
        {
            if (!Owns(db, user, group)) return Fail<bool>("Access denied.");
            if (!db.Accounts.Any(a => a.Id == student && a.Role == Roles.Student && a.GroupId == group) ||
                !db.Courses.Any(c => c.Id == course && c.GroupId == group)) return Fail<bool>("Select a student and course.");
            if (grade is < 0 or > 100) return Fail<bool>("Grade must be between 0 and 100.");
            var current = db.Marks.Find(student, course);
            if (grade is null) { if (current is not null) db.Marks.Remove(current); }
            else if (current is null) db.Marks.Add(new CourseMark { StudentId = student, CourseId = course, Grade = grade.Value });
            else current.Grade = grade.Value;
            return Yes();
        });
}
