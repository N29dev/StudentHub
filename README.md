# StudentHub

A local academic planner built with C# and Blazor for the Introduction to Functional Programming final project.

## Features

- Dashboard with upcoming deadlines, course progress and today's classes.
- Create, edit, complete, delete, search and filter assignments.
- Course management with credit-weighted GPA.
- Weekly timetable with overlapping lesson validation.
- Smart task ordering based on deadlines and importance.
- JSON persistence, responsive layouts and system light/dark theme.

## Run

Install the .NET 8 SDK (or a compatible newer SDK with the .NET 8 runtime).

    git clone https://github.com/N29dev/StudentHub.git
    cd StudentHub
    dotnet run --project StudentHub --urls http://localhost:5080

Open http://localhost:5080. The first launch shows example academic data. Changes are saved in StudentHub/App_Data/studenthub.json. To reset the demo, stop the application and delete that file (back it up first).

## Tests

    dotnet test StudentHub.Tests

## Structure

- StudentHub.Core: immutable records, Result<T>, pure functions, transformations, rules.
- StudentHub: Blazor pages, reusable components, responsive CSS and JSON store.
- StudentHub.Tests: xUnit tests for business rules and persistence.

The functional core does not access the file system, the clock or UI. Callers pass dates and IDs explicitly. The imperative shell handles input and storage. A fixed example 4.0 scale is used, not an official institutional calculation.

See [development and defense notes](docs/defense.md) and [manual QA checklist](docs/qa.md).

## Limitations

This is a local, single-profile application without accounts. A single server instance owns the JSON file. Multiple tabs read the shared snapshot but do not automatically receive live updates. Do not expose the development server publicly. JSON is convenient for the exam demo; a transactional database and authentication would be necessary for a multi-user deployment.
