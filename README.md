# StudentHub

An academic management system built with C#, Blazor and JSON for Introduction to Functional Programming.

## Run locally

Install the .NET 8 SDK (or a compatible newer SDK with the .NET 8 runtime).

    git clone https://github.com/N29dev/StudentHub.git
    cd StudentHub
    dotnet run --project StudentHub

Open http://localhost:5080. Demo mode starts automatically on localhost, including sample data and the lecturer registration code `12345678`. No environment variables are required.

## Ready-made demo data

The fictional dataset is created automatically once per campus.json file without overwriting existing accounts. The login page includes buttons to fill the student and lecturer demo credentials.

| Login | Role | Password |
| --- | --- | --- |
| lecturer@demo.test | Lecturer (IT-2507 and IT-2508) | Demo12345 |
| lecturer2@demo.test | Lecturer (SE-2503) | Demo12345 |
| student@demo.test | Student (IT-2507) | Demo12345 |
| student7@demo.test | Student (IT-2508) | Demo12345 |
| student13@demo.test | Student (SE-2503) | Demo12345 |

There are 18 students total (other logins are student2@demo.test through student18@demo.test), 2 lecturers, 3 groups, 15 courses, 45 assignments, 30 lessons and 72 course grades. Deadlines are relative to the date the dataset is first generated. GPA is calculated automatically; one course per student is ungraded. All names and records are fictional examples. Demo mode rejects non-loopback requests. For a non-demo installation, set StudentHub__SeedDemo=false and provide your own lecturer code; start with a separate, empty data file (disabling the flag does not delete previously created demo accounts). Never use shared demo passwords on a public deployment.

## Demo flow

1. Create a lecturer account (select Lecturer and enter the configured invitation code).
2. Open Manage groups, create a group such as IT-2507, and note its generated join code.
3. Add courses, publish assignments, and schedule lessons.
4. In a private browser window register a student using the group join code (or join later from the profile).
5. As the student, view tasks, mark completion, view timetable and use the grade predictor.
6. As the lecturer, refresh the group and use Students & grades to set a grade. The student can see their updated GPA after refreshing or navigating.
7. Edit a profile and optionally upload a PNG/JPEG avatar.

The app creates StudentHub/App_Data/campus.json on the first successful change. To reset the demo, stop the server and back up/delete that file. The earlier single-user JSON and SQLite database, if present, are preserved but not migrated. The application has no external NuGet dependencies; only the test project needs xUnit packages.

## Test

    dotnet test StudentHub.Tests/StudentHub.Tests.csproj

## Structure

- StudentHub.Core: immutable records, Result<T>, pure functions, transformations and academic rules.
- StudentHub: Blazor pages, reusable UI, account endpoints, authorization, JSON storage and application services.
- StudentHub.Tests: xUnit unit and integration tests.

The functional core does not access storage, the clock or the UI. The service maps stored records to immutable values, applies validation and saves only successful changes. A lock serializes writes; a temporary file replaces the saved snapshot only after serialization succeeds.

See [defense notes](docs/defense.md) and [manual QA](docs/qa.md).

## Limitations

This is an educational local demo with one lecturer owner per group and one group per student. It does not implement institution-wide enrollment, email verification, account recovery, multi-lecturer groups or automatic live updates. JSON storage is intended for one running server process and small datasets. No real student data is required. Use HTTPS, stronger operational controls and production-grade identity management before deploying online. The grading scale is illustrative. Assignment completion is a self-reported progress marker, not a file submission.
