using Microsoft.Extensions.Configuration;
using System.Text.Json;
using StudentHub.Core;
using StudentHub.Services;
using Xunit;

namespace StudentHub.Tests;

public class CampusTests
{
    [Fact]
    public void GroupWorkflowEnforcesRolesAndStudentIsolation()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "campus.json");
        try
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?> { ["StudentHub:LecturerCode"] = "invite-test" }).Build();
            var service = new CampusService(new CampusStore(path), config);
            const string password = "test-password";
            Assert.IsType<Fail<ProfileInfo>>(service.Register("Unauthorised", "bad@example.test", password, Roles.Lecturer, "wrong", ""));
            var teacher = Assert.IsType<Ok<ProfileInfo>>(service.Register("Teacher", "teacher@example.test", password, Roles.Lecturer, "invite-test", "")).Value;
            var otherTeacher = Assert.IsType<Ok<ProfileInfo>>(service.Register("Other", "other@example.test", password, Roles.Lecturer, "invite-test", "")).Value;
            Assert.IsType<Fail<ProfileInfo>>(service.Login(teacher.Email, "wrong-password"));
            Assert.IsType<Ok<ProfileInfo>>(service.Login(teacher.Email, password));
            var persisted = JsonSerializer.Deserialize<CampusData>(File.ReadAllText(path))!;
            Assert.NotEqual(password, persisted.Accounts.Single(a => a.Id == teacher.Id).PasswordHash);

            var group = Assert.IsType<Ok<Guid>>(service.CreateGroup(teacher.Id, "IT-2507")).Value;
            var code = Assert.Single(service.MyGroups(teacher.Id)).JoinCode;
            var student = Assert.IsType<Ok<ProfileInfo>>(service.Register("Student", "student@example.test", password, Roles.Student, "", code)).Value;
            var outsider = Assert.IsType<Ok<ProfileInfo>>(service.Register("Other student", "student2@example.test", password, Roles.Student, "", "")).Value;
            Assert.Equal<Guid?>(group, student.GroupId);
            var c = new Course(Guid.NewGuid(), "Functional Programming", 5, null);
            Assert.IsType<Fail<bool>>(service.SaveCourse(student.Id, group, c));
            Assert.IsType<Fail<bool>>(service.SaveCourse(otherTeacher.Id, group, c));
            Assert.IsType<Ok<bool>>(service.SaveCourse(teacher.Id, group, c));
            var task = new Assignment(Guid.NewGuid(), c.Id, "Final project", new DateOnly(2026,11,15), 3);
            Assert.IsType<Ok<bool>>(service.SaveAssignment(teacher.Id, group, task));
            Assert.Single(service.StudentData(student.Id).Assignments);
            Assert.Empty(service.StudentData(outsider.Id).Assignments);
            Assert.IsType<Fail<bool>>(service.Toggle(outsider.Id, task.Id));
            Assert.IsType<Ok<bool>>(service.Toggle(student.Id, task.Id));
            Assert.True(Assert.Single(service.StudentData(student.Id).Assignments).Completed);
            Assert.IsType<Fail<bool>>(service.SetGrade(student.Id, group, student.Id, c.Id, 100));
            Assert.IsType<Ok<bool>>(service.SetGrade(teacher.Id, group, student.Id, c.Id, 95));
            Assert.Equal<decimal?>(4m, service.StudentData(student.Id).Courses.Gpa());
            Assert.Equal(1, Assert.Single(service.ViewGroup(teacher.Id, group).Students).Completed);
            Assert.Empty(service.ViewGroup(otherTeacher.Id, group).Students);
            Assert.IsType<Ok<bool>>(service.JoinGroup(outsider.Id, code));
            Assert.False(Assert.Single(service.StudentData(outsider.Id).Assignments).Completed);
            Assert.Null(service.StudentData(outsider.Id).Courses.Gpa());
            var reloaded = new CampusService(new CampusStore(path), config);
            Assert.IsType<Ok<ProfileInfo>>(reloaded.Login(teacher.Email, password));
            Assert.True(Assert.Single(reloaded.StudentData(student.Id).Assignments).Completed);
            Assert.Equal<decimal?>(4m, reloaded.StudentData(student.Id).Courses.Gpa());
            Assert.IsType<Ok<bool>>(reloaded.DeleteAssignment(teacher.Id, group, task.Id));
            var saved = JsonSerializer.Deserialize<CampusData>(File.ReadAllText(path))!;
            Assert.Empty(saved.Completions);
        }
        finally
        {
            if (Directory.Exists(Path.GetDirectoryName(path)))
                Directory.Delete(Path.GetDirectoryName(path)!, true);
        }
    }

    [Fact]
    public void DemoSeedIsIdempotentAndPreservesRealAccounts()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "campus.json");
        try
        {
            var store = new CampusStore(path);
            var service = new CampusService(store, new ConfigurationBuilder().Build());
            var existing = Assert.IsType<Ok<ProfileInfo>>(service.Register(
                "Existing", "existing@example.test", "test-password", Roles.Student, "", "")).Value;
            var today = new DateOnly(2026, 11, 15);
            Assert.True(DemoData.Seed(store, today));
            Assert.False(DemoData.Seed(new CampusStore(path), today));
            var saved = JsonSerializer.Deserialize<CampusData>(File.ReadAllText(path))!;
            Assert.Equal(21, saved.Accounts.Count);
            Assert.Equal(3, saved.Groups.Count);
            Assert.Equal(15, saved.Courses.Count);
            Assert.Equal(45, saved.Assignments.Count);
            Assert.Equal(30, saved.Lessons.Count);
            Assert.Equal(72, saved.Marks.Count);
            Assert.NotNull(service.GetProfile(existing.Id));
            var student = Assert.IsType<Ok<ProfileInfo>>(service.Login("student@demo.test", DemoData.Password)).Value;
            var view = service.StudentData(student.Id);
            Assert.Equal(15, view.Assignments.Length);
            Assert.Equal(10, view.Lessons.Length);
            Assert.Equal(5, view.Assignments.Count(a => a.Completed));
            Assert.NotNull(view.Courses.Gpa());
            Assert.Equal(5, view.Courses.Length);
        }
        finally
        {
            if (Directory.Exists(Path.GetDirectoryName(path)))
                Directory.Delete(Path.GetDirectoryName(path)!, true);
        }
    }

    [Fact]
    public void GradePredictionHandlesFeasibilityAndInvalidValues()
    {
        var a = Assert.IsType<Ok<Prediction>>(GradePrediction.RequiredFinal(85, 40, 90)).Value;
        Assert.Equal(97.5m, a.Required);
        Assert.True(a.Possible);
        Assert.False(Assert.IsType<Ok<Prediction>>(GradePrediction.RequiredFinal(50, 40, 95)).Value.Possible);
        Assert.IsType<Fail<Prediction>>(GradePrediction.RequiredFinal(85, 0, 90));
    }
}
