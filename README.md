# StudentHub

An academic management system built with C#, Blazor and JSON for Introduction to Functional Programming.

## Run locally

Install the .NET 8 SDK (or a compatible newer SDK with the .NET 8 runtime).

    git clone https://github.com/N29dev/StudentHub.git
    cd StudentHub
    StudentHub__LecturerCode=local-demo-code dotnet run --project StudentHub --urls http://localhost:5080

On Windows PowerShell set `$env:StudentHub__LecturerCode = "local-demo-code"` first, then run the dotnet command. Open http://localhost:5080. Choose your own invitation code; the example is for a local demo, not a production secret.

## Demo flow

1. Create a lecturer account (select Lecturer and enter the configured invitation code).
2. Open Manage groups, create a group such as IT-2507, and note its generated join code.
3. Add courses, publish assignments, and schedule lessons.
4. In a private browser window register a student using the group join code (or join later from the profile).
5. As the student, view tasks, mark completion, view timetable and use the grade predictor.
6. As the lecturer, refresh the group and use Students & grades to set a grade. The student can see their updated GPA after refreshing or navigating.
7. Edit a profile and optionally upload a PNG/JPEG avatar.

The app creates StudentHub/App_Data/campus.json on the first successful change. To reset the demo, stop the server and back up/delete that file. The earlier single-user JSON and JSON storage, if present, are preserved but not migrated. The application has no external NuGet dependencies; only the test project needs xUnit packages.

## Test

    dotnet test StudentHub.Tests/StudentHub.Tests.csproj

## Structure

- StudentHub.Core: immutable records, Result<T>, pure functions, transformations and academic rules.
- StudentHub: Blazor pages, reusable UI, account endpoints, authorization, SQLite database and application services.
- StudentHub.Tests: xUnit unit and integration tests.

The functional core does not access storage, the clock or the UI. The service maps stored records to immutable values, applies validation and saves only successful changes. A lock serializes writes; a temporary file replaces the saved snapshot only after serialization succeeds.

See [defense notes](docs/defense.md) and [manual QA](docs/qa.md).

## Limitations

This is an educational local demo with one lecturer owner per group and one group per student. It does not implement institution-wide enrollment, email verification, account recovery, multi-lecturer groups or automatic live updates. JSON storage is intended for one running server process and small datasets. No real student data is required. Use HTTPS, stronger operational controls and production-grade identity management before deploying online. The grading scale is illustrative. Assignment completion is a self-reported progress marker, not a file submission.
