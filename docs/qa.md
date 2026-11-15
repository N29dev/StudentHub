# Manual QA checklist (to execute before submission)

Record pass/fail and actual browser/device for every item. Do not mark tests passed until performed.

- [ ] Launch using the README commands.
- [ ] Open overview, assignments, courses and timetable; verify navigation.
- [ ] Add a course; reject duplicate names and out-of-range grades.
- [ ] Edit credits and grade; verify GPA recalculation.
- [ ] Create and edit assignments; search by title and filter by course/status.
- [ ] Complete and reopen an assignment; verify dashboard counts.
- [ ] Sort by smart priority; inspect overdue and upcoming items.
- [ ] Add a lesson; reject invalid times and overlaps; allow adjacent periods.
- [ ] Delete a lesson and assignment; delete their course only after dependencies are removed.
- [ ] Restart the server; verify persisted changes.
- [ ] Check narrow mobile viewport (~375px), desktop and keyboard navigation.
- [ ] Check two browsers and system light/dark mode.
- [ ] Run dotnet test StudentHub.Tests and record the output.
