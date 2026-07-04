# TaskTracker — Code Review

**Date:** 2026-07-03 · **Scope:** entire app (all uncommitted work) · **Spec:** `TaskTracker/requirement.md` (roadmap.sh Task Tracker CLI)

Reviewed by reading every file and by actually running the app (`dotnet build` + executing the built CLI against real, missing, and corrupted `tasks.json` files). Findings marked **[verified at runtime]** were reproduced with real commands.

---

## Verdict

**Solid foundation for a learning project — the structure is genuinely good, but error handling is the big gap.**

The happy paths all work: add, update, delete, mark-in-progress, mark-done, and all four list variants behave correctly. The project is organized the way a much larger app would be (Models / Services / Enums / Interfaces separated), which most beginners don't do. The main problem is that every failure path that goes through `TaskService` crashes with a raw .NET stack trace, which directly violates the requirement to "handle errors and edge cases gracefully." Fix the error boundary and this goes from "works when you're nice to it" to "actually robust."

| Criterion | Score | Notes |
|---|---|---|
| Correctness (happy paths) | 8/10 | All commands work as specced |
| Error handling | 3/10 | Service-layer errors crash with stack traces |
| Architecture / separation of concerns | 8/10 | Clean layering, right instincts |
| Readability & style | 7/10 | Consistent shape, some duplication and dead code |
| Idiomatic modern C# | 6/10 | Good pattern-matching/collection expressions; `.Result` and `String` are anti-patterns |
| Spec compliance | 7/10 | Two deviations (status format, file creation) |
| Testing & docs | 2/10 | No tests, no README (spec asks for a README) |

**Overall: ~6.5/10** — above average for a first CLI project. The issues below are all fixable, and most are one concept each.

---

## What's good 👍

These are worth calling out explicitly, because they're habits to keep:

1. **Real layering.** CLI parsing lives in `Program.cs`, business logic and persistence in `TaskService`, data shape in `Models/AppTask.cs`, statuses in an enum. Most solutions to this exercise are one 200-line `Main`. Yours would survive growing into a bigger app.
2. **You validate input at all.** `ValidateArgs` for argument counts, `int.TryParse` for IDs, empty-description checks in the service. Both layers defend themselves — the instinct is right (the *duplication* of it is the part to refine, see D3).
3. **Modern C# features used naturally:** top-level statements, target-typed `new()`, collection expressions (`[]`), pattern matching (`is not null`), `await using` for stream disposal, `static` lambda in `OrderBy`.
4. **UTC for storage.** Storing `DateTime.UtcNow` in the JSON is the correct call (the *display* inconsistency is bug B3, but the storage decision is right).
5. **Readable persistence.** `WriteIndented = true` + camelCase naming policy means `tasks.json` is human-inspectable, which the spec's "look at the JSON file to verify" step wants.
6. **Sensible exit codes** on the paths you control: `ShowError` exits 1, success exits 0. Scripts can rely on that — except when the app crashes (B1).
7. **A `help` command** the spec didn't even ask for.

---

## Bugs — must fix 🐞

### B1. Service-layer errors crash the app with raw stack traces **[verified at runtime]**

`Program.cs` never catches exceptions, and `TaskService` communicates all failures by throwing. Every one of these crashes with `Unhandled exception. System.AggregateException …` and a stack trace:

```
dotnet run -- update 999 "x"     # KeyNotFoundException → crash
dotnet run -- mark-done 999      # KeyNotFoundException → crash
dotnet run -- delete 999         # KeyNotFoundException → crash
dotnet run -- add ""             # ArgumentException → crash
dotnet run -- list               # with a corrupted tasks.json → JsonException → crash
```

This is the single biggest violation of the spec ("Ensure to handle errors and edge cases gracefully"). A user who typos an ID should see `Error: Task with ID 999 not found.`, not a stack trace.

**Fix:** one error boundary at the top. Wrap the command dispatch in `try/catch`, print `ex.Message` via `ShowError`, done:

```csharp
try
{
    await RunCommand(args);
}
catch (Exception ex) when (ex is KeyNotFoundException or ArgumentException or JsonException)
{
    ShowError(ex.Message);
}
```

(Together with B5 — once you `await` instead of `.Result`, you catch the real exception instead of an `AggregateException` wrapper.)

### B2. `delete`'s "Task not found" branch is dead code — the contract is broken

`Program.cs:112-120` checks `DeleteTask`'s `bool` result and has an `else` that prints "Task not found". But `TaskService.DeleteTask` (`TaskService.cs:119-122`) **throws** `KeyNotFoundException` when the ID doesn't exist — it can never return `false`. So the graceful branch you wrote is unreachable, and the actual behavior is the B1 crash. **[verified at runtime]**

**Fix:** pick one contract and commit to it. Either `DeleteTask` returns `false` (delete the `throw`), or it throws and `Program.cs` drops the `if/else` and relies on the B1 catch. Mixing "returns bool" *and* "throws" is the worst of both.

### B3. Inconsistent timezones in `list` output **[verified at runtime]**

`Program.cs:173` prints `task.CreatedAt` raw (UTC) but `task.UpdatedAt.ToLocalTime()` (local). In your timezone (UTC+7) the actual output was:

```
ID    Description      Status      Created At              Updated At
4     finish the cli   Done        7/2/2026 6:14:47 PM     7/3/2026 1:21:24 AM
```

Those two timestamps are 7 hours apart in *presentation only* — a task can appear to be updated "before" it was created.

**Fix:** convert both: `task.CreatedAt.ToLocalTime()`.

### B4. `launchSettings.json` hijacks `dotnet run` **[verified at runtime]**

`Properties/launchSettings.json` hardcodes `"commandLineArgs": "list in-progress"`. When I ran `dotnet run -- list`, the profile args won and the app filtered to in-progress — silently showing 1 task instead of 2. This will sabotage your own manual testing (and did, during this review).

**Fix:** delete the `commandLineArgs` line (it's a Visual Studio debug-profile convenience, not something the app needs), or always test with `dotnet run --no-launch-profile -- <args>`.

### B5. Sync-over-async: `.Result` everywhere

Every service call in `Program.cs` (lines 24, 31, 49, 65, 81, 97, 113) blocks on `.Result`. In a console app this won't deadlock, but it's the #1 async anti-pattern in C#: it wraps exceptions in `AggregateException` (which is why B1's crashes are so ugly), and it will deadlock the day you copy this pattern into ASP.NET or a UI app.

**Fix:** top-level statements can `await` directly. Make `RunCommand` `async Task`, then `var tasks = await taskService.GetTasks();`. This is the highest-value C# lesson in this whole review.

---

## Spec deviations ⚠️

### S1. Status is stored as a number, not `todo` / `in-progress` / `done`

The spec defines status values as `todo`, `in-progress`, `done`. Your `tasks.json` stores `"status": 1`. Two problems:

- The file isn't self-describing — a human (or another tool) reading it can't tell what `1` means.
- **Fragility:** if you ever reorder or insert a value in the `Status` enum, every saved task silently changes meaning on next load. That's a data-corruption bug waiting for a refactor.

**Fix (one line):** add an enum-as-string converter to `_jsonOption` in `TaskService.cs:14`:

```csharp
Converters = { new JsonStringEnumConverter(JsonNamingPolicy.KebabCaseLower) }
```

`KebabCaseLower` makes it serialize exactly as the spec spells it: `"todo"`, `"in-progress"`, `"done"`. (You'll need a small migration or a fresh `tasks.json` since existing files hold numbers.)

### S2. The JSON file is not created if it doesn't exist (on read) **[verified at runtime]**

Spec: "The JSON file should be created if it does not exist." In a fresh directory, `list` runs fine but creates nothing; the file only appears on the first `add`. This is a mild deviation (reads are handled gracefully), but it's literally in the constraints list — create it in `LoadTasksFromFile` when missing, or save an empty list on startup.

### S3. No README

The spec's "Finalizing the Project" section asks for a README explaining usage. Cheap to write, and it's the first thing anyone (including future-you) looks at.

---

## Design & code-quality improvements 🔧

### D1. The interface and the implementation have drifted

`ITaskService.GetTasks(string listType)` has no default value; `TaskService.GetTasks(string listType = "all")` does. Default parameter values are resolved against the *compile-time* type in C#, so the moment you refactor to `ITaskService taskService = new TaskService();` (the natural next step — it's why the interface exists), `taskService.GetTasks()` stops compiling.

Also worth being honest about: **nothing currently uses `ITaskService`.** `Program.cs:16` instantiates the concrete class. That's fine for a learning project — but either use the interface as the variable type (and align the signatures), or delete it until you need it (e.g., when you write tests and want a fake). An abstraction nobody consumes is maintenance cost with no payoff.

### D2. `TaskService` should speak `Status`, not magic strings

`GetTasks(string listType)` string-matches `"todo"` / `"in-progress"` / `"done"` — the same literals `Program.cs:29` already validated. The set now lives in two files that must stay in sync by hand, and if a future caller passes an unknown string, the service's `default:` branch **silently returns all tasks** instead of erroring.

**Fix:** parse once at the CLI boundary, pass the enum through:

```csharp
// Program.cs — the CLI owns string→enum
if (!TryParseStatus(args[1], out Status status)) { ShowError(...); }
var tasks = await taskService.GetTasks(status);

// ITaskService — the service speaks the domain type
Task<List<AppTask>> GetTasks(Status? filter = null);
```

Now adding a status is a compiler-checked change, and invalid input can't reach the service at all.

### D3. Copy-paste blocks in the command switch

The `ValidateArgs` + `int.TryParse` + `ShowError` skeleton is repeated verbatim in `update`, `mark-in-progress`, `mark-done`, and `delete` (Program.cs:59, 75, 91, 107). Any fix to ID parsing has to be made four times. Extract a helper in the same spirit as your existing `ValidateArgs`:

```csharp
static bool TryGetTaskId(string[] args, out int taskId) { ... }
```

Also: `mark-in-progress` and `mark-done` are 95% identical — they can share one path that differs only in the `Status` value.

### D4. Dead code (three instances)

- **`TaskService.cs:61-64`** — the duplicate-ID check in `AddTask` can never fire: `GenerateTaskId` just computed `max + 1` from the *same in-memory list* two lines earlier. It reads like race-condition handling, but there's no concurrency here. Delete it.
- **`Program.cs:144-148`** — `ValidateArgs`'s `args.Length < 1` check is unreachable: `RunCommand` is only called when `args.Length > 0`, and the `switch` already dereferenced `args[0]`.
- **`Program.cs:29`** — the `args.Length > 1 &&` guard is always true at that point (the `args.Length == 1` case already `break`-ed above).

Dead checks aren't harmless — they tell the next reader a condition is possible when it isn't.

### D5. `GenerateTaskId` sorts the whole list to read one value

`tasks.OrderBy(t => t.Id).LastOrDefault()` is O(n log n) to find a maximum. Simpler and cheaper:

```csharp
return tasks.Count == 0 ? 1 : tasks.Max(t => t.Id) + 1;
```

(Minor at this scale — the lesson is "say what you mean": *Max*, not *sort-then-take-last*.)

### D6. Whitespace-only descriptions are accepted

`taskDescription == null || taskDescription.Length < 1` (TaskService.cs:45 and :75) lets `add "   "` create a blank-looking task. The BCL has the exact tool: `string.IsNullOrWhiteSpace(taskDescription)`.

### D7. `list` argument validation is inconsistent with the other commands

`add "x" y` errors with "Too many arguments", but `list done extra garbage` silently ignores the extras. Run `list` through the same `ValidateArgs` treatment for consistency.

---

## Style nits 🧹

- **`String` vs `string`:** the codebase mixes `String[] args` / `String message` with `string` elsewhere. Idiomatic C# uses the lowercase `string` keyword everywhere. Pick one (pick `string`).
- **Errors go to stdout:** `ShowError` uses `Console.WriteLine`; convention is `Console.Error.WriteLine` so scripts can separate output from errors.
- **The `"Task Tracker CLI"` banner prints on every run,** including before errors and in `list` output that someone might pipe. Consider dropping it or printing it only for `help`.
- **Unused usings:** `System`, `System.Collections.Generic`, `System.Text` at the top of most files are redundant — `ImplicitUsings` is enabled in the csproj. Your IDE's "remove unnecessary usings" will clean these.
- **`tasks.json` should probably be gitignored** — it's runtime user data, not source. Same question for `Properties/launchSettings.json` (per-developer IDE config; teams differ, but if you keep it, fix B4).
- **`Environment.Exit` inside `ShowError`** works, but it hides control flow — a reader of the `list` case can't see that line 37 terminates the process. Returning a bool/exit-code from `RunCommand` and exiting once in `Main` is the more transparent shape. (Fine to defer; just know why.)

---

## Requirement compliance checklist

| Requirement | Status |
|---|---|
| Add / Update / Delete tasks | ✅ |
| Mark in-progress / done | ✅ |
| List all / done / todo / in-progress | ✅ |
| Positional CLI arguments | ✅ |
| JSON file in current directory | ✅ |
| JSON file created if not exists | ⚠️ only on `add`, not on `list` (S2) |
| Native filesystem APIs, no external libraries | ✅ |
| Handle errors and edge cases gracefully | ❌ crashes on every service-level error (B1) |
| Task properties: id, description, status, createdAt, updatedAt | ✅ present; ⚠️ status stored as number, spec says todo/in-progress/done (S1) |
| README | ❌ missing (S3) |

---

## Suggested order of attack

1. **B5 → B1 together:** make `RunCommand` async, `await` everything, add one `try/catch` boundary. (~30 min, transforms the app's robustness and teaches the most important C# async lesson.)
2. **B2:** pick one error contract for `DeleteTask`.
3. **S1:** `JsonStringEnumConverter(JsonNamingPolicy.KebabCaseLower)`.
4. **B3, B4:** two one-line fixes.
5. **D2:** move status parsing to the CLI boundary, pass `Status?` to the service — this also deletes the D4/D7 duplication naturally.
6. **D3–D7 + style nits** as a cleanup pass.
7. **S3 + tests:** write the README; then, as a stretch goal, add an xUnit project and test `TaskService` against a temp file — you'll immediately feel why `ITaskService` (D1) earns its keep once a test wants to exist.

## Top 5 takeaways for your C# journey

1. **Never `.Result` / `.Wait()` on async code — `await` it.** Exceptions unwrap cleanly and the pattern stays safe in every runtime context.
2. **Decide where errors become messages.** Inner layers throw (or return results); *one* outer boundary translates to user-facing text and exit codes. Right now you have half of each.
3. **Contracts must match behavior.** A method that returns `bool` for "not found" must never *throw* for "not found" (B2). Callers code against the signature.
4. **Parse, don't validate twice.** Turn strings into types (`Status`, `int`) once, at the edge; pass types inward. Duplicate string-matching in two layers always drifts.
5. **Serialize enums as strings.** Numeric enum persistence breaks silently when the enum changes; string persistence is self-describing and refactor-safe.

---

*Environment note from the review session: your **C: drive has ~0 GB free**, which caused `dotnet` itself to fail intermittently ("There is not enough space on the disk") during testing. Worth cleaning up before it corrupts a build or NuGet cache.*
