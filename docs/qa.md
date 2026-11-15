# Manual QA checklist

Record the actual browser/device and result. Only mark checks after performing them.

- [ ] Run the application and register a lecturer with a valid invitation code.
- [ ] Reject an invalid lecturer code, duplicate email and wrong login password.
- [ ] Create a group, add a course, and publish an assignment.
- [ ] Register a student with the group code in a separate browser session.
- [ ] Verify students cannot open lecturer management or edit official grades.
- [ ] Complete/reopen assignments; test search, course/status filter and priority sorting.
- [ ] Verify another student has independent completion status and grades.
- [ ] Add a timetable lesson, reject overlap, allow back-to-back times.
- [ ] Save a grade as lecturer; check the student GPA after refresh.
- [ ] Edit/delete course, lesson and task; confirm dependencies are handled.
- [ ] Check grade prediction with possible, impossible and invalid target inputs.
- [ ] Edit profile, upload PNG/JPEG, reject oversized file, remove picture.
- [ ] Sign out, then try opening a protected page.
- [ ] Restart server and verify SQLite data persists.
- [ ] Check desktop/mobile viewport, two browsers, keyboard and system light/dark mode.
- [ ] Run dotnet test StudentHub.Tests/StudentHub.Tests.csproj and record results.
