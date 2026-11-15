using Microsoft.AspNetCore.Identity;

namespace StudentHub.Services;

// Fictional sample data for the exam demonstration. Enabled by default for the local exam demo.
public static class DemoData
{
    public const string Password = "Demo12345";

    public static bool Seed(CampusStore store, DateOnly today)
    {
        var data = store.Copy();
        if (data.DemoSeeded) return false;

        // Never take over an existing account, including one with a reserved demo email.
        var emails = new[] { "lecturer@demo.test", "lecturer2@demo.test", "student@demo.test" }
            .Concat(Enumerable.Range(2, 18).Select(i => $"student{i}@demo.test"))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (data.Accounts.Any(a => emails.Contains(a.Email)))
            throw new InvalidOperationException("A demo email is already in use. Demo data was not added.");

        var hasher = new PasswordHasher<CampusAccount>();

        CampusAccount AddAccount(string name, string email, string role, Guid? group, string program)
        {
            var account = new CampusAccount
            {
                Id = Guid.NewGuid(), Name = name, Email = email, Role = role,
                GroupId = group, Program = program
            };
            account.PasswordHash = hasher.HashPassword(account, Password);
            data.Accounts.Add(account);
            return account;
        }

        var teacher = AddAccount("Aida Sarsen", "lecturer@demo.test", Roles.Lecturer, null, "School of Computer Science");
        var teacher2 = AddAccount("Daniyar Bekov", "lecturer2@demo.test", Roles.Lecturer, null, "School of Digital Technologies");

        var names = new[]
        {
            "Arman Nurgali", "Amina Tolegen", "Dias Karim", "Aruzhan Sadyk", "Miras Omar", "Dana Serik",
            "Alikhan Aman", "Kamila Rakhim", "Adil Zhan", "Madina Askar", "Timur Kassym", "Aigerim Nur",
            "Sultan Ilyas", "Ayana Tursun", "Nursultan Baim", "Aisha Kenes", "Rauan Bektas", "Zarina Kair"
        };

        var courses = new (string Name, int Credits, string[] Tasks)[]
        {
            ("Functional Programming", 5, ["LINQ exercises", "Result and Bind practice", "Final project"]),
            ("Discrete Mathematics", 5, ["Graph theory problems", "Combinatorics worksheet", "Recurrence relations"]),
            ("Computer Networks", 5, ["Network topology lab", "IP addressing practice", "Routing quiz"]),
            ("Software Engineering", 4, ["UML class diagram", "Requirements analysis", "Sprint report"]),
            ("Academic English", 3, ["Vocabulary review", "Research abstract", "Presentation slides"])
        };
        var groups = new[] { "IT-2507", "IT-2508", "SE-2503" };
        var random = new Random(2507);

        int studentNumber = 0;
        for (int g = 0; g < groups.Length; g++)
        {
            string joinCode = $"DEMO{g + 1:00}";
            while (data.Groups.Any(x => x.JoinCode == joinCode))
                joinCode = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(5));
            var group = new AcademicGroup
            {
                Id = Guid.NewGuid(), Name = groups[g], JoinCode = joinCode,
                OwnerId = g < 2 ? teacher.Id : teacher2.Id
            };
            data.Groups.Add(group);

            var students = new List<CampusAccount>();
            for (int s = 0; s < 6; s++)
            {
                studentNumber++;
                var email = studentNumber == 1 ? "student@demo.test" : $"student{studentNumber}@demo.test";
                students.Add(AddAccount(names[studentNumber - 1], email, Roles.Student, group.Id,
                    g == 2 ? "Software Engineering" : "Information Technology"));
            }

            var offerings = new List<CourseOffering>();
            for (int c = 0; c < courses.Length; c++)
            {
                var spec = courses[c];
                var course = new CourseOffering
                {
                    Id = Guid.NewGuid(), GroupId = group.Id, Name = spec.Name, Credits = spec.Credits
                };
                data.Courses.Add(course);
                offerings.Add(course);

                for (int t = 0; t < spec.Tasks.Length; t++)
                {
                    int offset = t switch { 0 => -7 - c, 1 => c - 1, _ => 3 + c * 2 };
                    var task = new PublishedAssignment
                    {
                        Id = Guid.NewGuid(), CourseId = course.Id, Title = spec.Tasks[t],
                        Due = today.AddDays(offset), Importance = t == 2 ? 3 : 2
                    };
                    data.Assignments.Add(task);
                    foreach (var student in students)
                    {
                        bool completed = student.Id == students[0].Id
                            ? t == 0 && c != 2 || t == 1 && c == 0
                            : random.Next(100) < (t == 0 ? 85 : t == 1 ? 55 : 15);
                        if (completed) data.Completions.Add(new Completion
                        { StudentId = student.Id, AssignmentId = task.Id });
                    }
                }

                foreach (var student in students)
                {
                    // Leave the last course ungraded; GPA uses only graded credits.
                    if (c == courses.Length - 1) continue;
                    int[] demoGrades = [94, 87, 78, 91];
                    var grade = student.Id == students[0].Id ? demoGrades[c] : random.Next(65, 100);
                    data.Marks.Add(new CourseMark { StudentId = student.Id, CourseId = course.Id, Grade = grade });
                }
            }

            // A consistent 5-day schedule with two classes per weekday.
            var week = new (DayOfWeek Day, int Course, int Hour, int Minute, string Room)[]
            {
                (DayOfWeek.Monday, 0, 9, 0, "B2.1"), (DayOfWeek.Monday, 1, 11, 0, "B3.2"),
                (DayOfWeek.Tuesday, 2, 10, 0, "C1.4"), (DayOfWeek.Tuesday, 4, 13, 0, "A2.1"),
                (DayOfWeek.Wednesday, 3, 9, 0, "B1.5"), (DayOfWeek.Wednesday, 0, 11, 0, "B2.1"),
                (DayOfWeek.Thursday, 1, 10, 0, "B3.2"), (DayOfWeek.Thursday, 2, 13, 0, "C1.4"),
                (DayOfWeek.Friday, 4, 9, 0, "A2.1"), (DayOfWeek.Friday, 3, 11, 0, "B1.5")
            };
            foreach (var slot in week)
            {
                var start = new TimeOnly(slot.Hour, slot.Minute).AddHours(g == 1 ? 4 : 0);
                data.Lessons.Add(new ScheduledLesson
                {
                    Id = Guid.NewGuid(), CourseId = offerings[slot.Course].Id,
                    Day = slot.Day, Start = start, End = start.AddMinutes(80),
                    Room = g == 2 ? $"D{slot.Room[1..]}" : slot.Room
                });
            }
        }

        data.DemoSeeded = true;
        store.Save(data);
        return true;
    }
}
