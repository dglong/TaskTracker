# Task Tracker CLI

A simple command-line application for tracking tasks and managing your to-do list. Tasks are stored in a JSON file (`tasks.json`) in the current directory. Built with C# / .NET 10 using only the standard library — no external dependencies.

This is an implementation of the [roadmap.sh Task Tracker](https://roadmap.sh/projects/task-tracker) project.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download) or later

## Getting started

Clone the repository and build the project:

```bash
git clone <repository-url>
cd TaskTracker
dotnet build
```

Run commands with `dotnet run --` followed by the command and its arguments:

```bash
dotnet run -- add "Buy groceries"
```

> **Note:** everything after `--` is passed to the app. The examples below use `task-cli` as shorthand for `dotnet run --`.

## Usage

### Add a task

```bash
task-cli add "Buy groceries"
# Task added: 1 - Buy groceries
```

New tasks start with a status of `todo`.

### Update a task's description

```bash
task-cli update 1 "Buy groceries and cook dinner"
# Task updated: 1 - Buy groceries and cook dinner
```

### Delete a task

```bash
task-cli delete 1
# Task deleted: 1
```

### Mark a task's status

```bash
task-cli mark-in-progress 1
# Task marked as In Progress: 1 - Buy groceries

task-cli mark-done 1
# Task marked as Done: 1 - Buy groceries
```

### List tasks

List all tasks:

```bash
task-cli list
```

List tasks filtered by status (`todo`, `in-progress`, or `done`):

```bash
task-cli list todo
task-cli list in-progress
task-cli list done
```

Output is rendered as a table:

```
ID    Description                                        Status     Created At                Updated At
------------------------------------------------------------------------------------------------------------------------
1     Buy groceries                                      Todo       7/4/2026 11:53:27 PM      7/4/2026 11:53:27 PM
```

### Help

```bash
task-cli help
```

## Commands

| Command | Arguments | Description |
|---------|-----------|-------------|
| `add` | `<description>` | Add a new task |
| `update` | `<id> <description>` | Update an existing task's description |
| `delete` | `<id>` | Delete a task |
| `mark-in-progress` | `<id>` | Mark a task as in progress |
| `mark-done` | `<id>` | Mark a task as done |
| `list` | `[todo\|in-progress\|done]` | List all tasks, or filter by status |
| `help` | | Show available commands |

## Data storage

Tasks are persisted to `tasks.json` in the current working directory. The file is created automatically on first use. Each task has the following properties:

| Property | Description |
|----------|-------------|
| `id` | Unique identifier for the task |
| `description` | Short description of the task |
| `status` | `todo`, `in-progress`, or `done` |
| `createdAt` | UTC timestamp when the task was created |
| `updatedAt` | UTC timestamp when the task was last updated |

Saves are **atomic**: data is written to a temporary file and then moved into place, so an interrupted write (crash, full disk, power loss) cannot corrupt or empty an existing `tasks.json`.

## Exit codes

| Code | Meaning |
|------|---------|
| `0` | Command succeeded |
| `1` | An error occurred (invalid input, task not found, etc.) |

Error messages are written to **stderr**, so they stay visible even when standard output is redirected to a file.

## Project structure

```
TaskTracker/
├── Program.cs              # Entry point and single error/exit boundary
├── CommandRunner.cs        # Parses arguments and dispatches commands
├── Interfaces/
│   └── ITaskService.cs     # Task service contract
├── Services/
│   └── TaskService.cs      # Task CRUD logic and JSON persistence
├── Models/
│   └── AppTask.cs          # Task model
└── Enums/
    └── Status.cs           # Task status (Todo, InProgress, Done)
```
