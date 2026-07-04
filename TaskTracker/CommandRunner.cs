using TaskTracker.Enums;
using TaskTracker.Interfaces;
using TaskTracker.Models;
using TaskTracker.Services;

namespace TaskTracker
{
    public class CommandRunner
    {
        private readonly ITaskService _taskService;

        public CommandRunner(ITaskService taskService)
        {
            _taskService = taskService;
        }

        public async Task RunCommand(string[] args)
        {
            switch (args[0])
            {
                case "list":
                    {
                        var isValid = ValidateArgs(args, 1, 2);
                        if (!isValid)
                        {
                            return;
                        }

                        if (args.Length == 1)
                        {
                            var tasks = await _taskService.GetTasks();
                            PrintTasksToCLI(tasks);
                            break;
                        }

                        if (TaskService.TryParseStatus(args[1], out Status status))
                        {
                            var tasks = await _taskService.GetTasks(status);
                            PrintTasksToCLI(tasks);
                            break;

                        }

                        ShowError("Invalid list type: " + args[1]);
                        break;
                    }
                case "add":
                    {
                        var isValid = ValidateArgs(args, 2, 2);
                        if (!isValid)
                        {
                            return;
                        }

                        var task = await _taskService.AddTask(args[1]);
                        Console.WriteLine($"Task added: {task.Id} - {task.Description}");
                        break;
                    }
                case "update":
                    {
                        var isValid = ValidateArgs(args, 3, 3);
                        if (!isValid)
                        {
                            return;
                        }
                        if (!TryParseTaskId(args[1], out int taskId))
                        {
                            ShowError("Invalid task ID: " + args[1]);
                            return;
                        }
                        var task = await _taskService.UpdateTask(taskId, args[2]);
                        Console.WriteLine($"Task updated: {task.Id} - {task.Description}");
                        break;
                    }
                case "mark-in-progress":
                    {
                        var isValid = ValidateArgs(args, 2, 2);
                        if (!isValid)
                        {
                            return;
                        }
                        if (!TryParseTaskId(args[1], out int taskId))
                        {
                            ShowError("Invalid task ID: " + args[1]);
                            return;
                        }
                        var task = await _taskService.UpdateTaskStatus(taskId, TaskTracker.Enums.Status.InProgress);
                        Console.WriteLine($"Task marked as In Progress: {task.Id} - {task.Description}");
                        break;
                    }
                case "mark-done":
                    {
                        var isValid = ValidateArgs(args, 2, 2);
                        if (!isValid)
                        {
                            return;
                        }
                        if (!TryParseTaskId(args[1], out int taskId))
                        {
                            ShowError("Invalid task ID: " + args[1]);
                            return;
                        }
                        var task = await _taskService.UpdateTaskStatus(taskId, TaskTracker.Enums.Status.Done);
                        Console.WriteLine($"Task marked as Done: {task.Id} - {task.Description}");
                        break;
                    }
                case "delete":
                    {
                        var isValid = ValidateArgs(args, 2, 2);
                        if (!isValid)
                        {
                            return;
                        }
                        if (!TryParseTaskId(args[1], out int taskId))
                        {
                            ShowError("Invalid task ID: " + args[1]);
                            return;
                        }
                        var result = await _taskService.DeleteTask(taskId);
                        if (result)
                        {
                            Console.WriteLine($"Task deleted: {taskId}");
                        }
                        else
                        {
                            ShowError("Task not found: " + taskId);
                        }
                        break;
                    }
                case "help":
                    {
                        Console.WriteLine("Available commands:");
                        Console.WriteLine("list [todo|in-progress|done] - List tasks, optionally filtered by status");
                        Console.WriteLine("add <task description> - Add a new task");
                        Console.WriteLine("update <task id> <new description> - Update an existing task's description");
                        Console.WriteLine("mark-in-progress <task id> - Mark a task as In Progress");
                        Console.WriteLine("mark-done <task id> - Mark a task as Done");
                        Console.WriteLine("delete <task id> - Delete a task");
                        break;
                    }
                default:
                    {
                        ShowError("Unknown command: " + args[0]);
                        break;
                    }
            }
        }

        static bool ValidateArgs(string[] args, int minLength, int maxLength)
        {
            if (args.Length < minLength)
            {
                ShowError("Not enough arguments provided for command: " + args[0]);
                return false;

            }

            if (args.Length > maxLength)
            {
                ShowError("Too many arguments provided for command: " + args[0]);
                return false;
            }

            return true;
        }

        static bool TryParseTaskId(string arg, out int taskId)
        {
            if (!int.TryParse(arg, out int id))
            {
                taskId = default;
                return false;
            }
            taskId = id;
            return true;
        }

        static void PrintTasksToCLI(List<AppTask> tasks)
        {
            Console.WriteLine($"{"ID",-5} {"Description",-50} {"Status",-10} {"Created At",-25} {"Updated At",-25}");
            Console.WriteLine(new string('-', 120));

            foreach (var task in tasks)
            {
                Console.WriteLine($"{task.Id,-5} {task.Description,-50} {task.Status,-10} {task.CreatedAt.ToLocalTime(),-25} {task.UpdatedAt.ToLocalTime(),-25}");
            }
        }

        public static void ShowError(string message)
        {
            Console.WriteLine("Error: " + message);
            Environment.Exit(1);
        }

    }
}
