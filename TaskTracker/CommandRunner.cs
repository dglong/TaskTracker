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

        public async Task<int> RunCommand(string[] args)
        {
            switch (args[0])
            {
                case "list":
                    {
                        if (!ValidateArgs(args, 1, 2))
                        {
                            return 1;
                        }

                        if (args.Length == 1)
                        {
                            var tasks = await _taskService.GetTasks();
                            PrintTasksToCLI(tasks);
                            return 0;
                        }

                        if (TaskService.TryParseStatus(args[1], out Status status))
                        {
                            var tasks = await _taskService.GetTasks(status);
                            PrintTasksToCLI(tasks);
                            return 0;
                        }

                        return ShowError("Invalid list type: " + args[1]);
                    }
                case "add":
                    {
                        if (!ValidateArgs(args, 2, 2))
                        {
                            return 1;
                        }

                        var task = await _taskService.AddTask(args[1]);
                        Console.WriteLine($"Task added: {task.Id} - {task.Description}");
                        return 0;
                    }
                case "update":
                    {
                        if (!ValidateArgs(args, 3, 3))
                        {
                            return 1;
                        }
                        if (!TryParseTaskId(args[1], out int taskId))
                        {
                            return ShowError("Invalid task ID: " + args[1]);
                        }
                        var task = await _taskService.UpdateTask(taskId, args[2]);
                        Console.WriteLine($"Task updated: {task.Id} - {task.Description}");
                        return 0;
                    }
                case "mark-in-progress":
                    {
                        if (!ValidateArgs(args, 2, 2))
                        {
                            return 1;
                        }
                        if (!TryParseTaskId(args[1], out int taskId))
                        {
                            return ShowError("Invalid task ID: " + args[1]);
                        }
                        var task = await _taskService.UpdateTaskStatus(taskId, TaskTracker.Enums.Status.InProgress);
                        Console.WriteLine($"Task marked as In Progress: {task.Id} - {task.Description}");
                        return 0;
                    }
                case "mark-done":
                    {
                        if (!ValidateArgs(args, 2, 2))
                        {
                            return 1;
                        }
                        if (!TryParseTaskId(args[1], out int taskId))
                        {
                            return ShowError("Invalid task ID: " + args[1]);
                        }
                        var task = await _taskService.UpdateTaskStatus(taskId, TaskTracker.Enums.Status.Done);
                        Console.WriteLine($"Task marked as Done: {task.Id} - {task.Description}");
                        return 0;
                    }
                case "delete":
                    {
                        if (!ValidateArgs(args, 2, 2))
                        {
                            return 1;
                        }
                        if (!TryParseTaskId(args[1], out int taskId))
                        {
                            return ShowError("Invalid task ID: " + args[1]);
                        }
                        await _taskService.DeleteTask(taskId);
                        Console.WriteLine($"Task deleted: {taskId}");
                        return 0;
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
                        return 0;
                    }
                default:
                    {
                        return ShowError("Unknown command: " + args[0]);
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

        public static int ShowError(string message)
        {
            Console.Error.WriteLine("Error: " + message);
            return 1;
        }

    }
}
