using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TaskTracker.Enums;
using TaskTracker.Interfaces;
using TaskTracker.Models;

namespace TaskTracker.Services
{
    public class TaskService(string filePath = "tasks.json") : ITaskService
    {
        private readonly string filePath = filePath;
        
        private readonly JsonSerializerOptions _jsonOption = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.KebabCaseLower) }
        };

        public async Task<List<AppTask>> GetTasks(Status? filter = null)
        {
            List<AppTask> tasks = await LoadTasksFromFile();

            switch (filter)
            {
                case Enums.Status.Todo:
                    tasks = tasks.FindAll(t => t.Status == Enums.Status.Todo);
                    return tasks;
                case Enums.Status.InProgress:
                    tasks = tasks.FindAll(t => t.Status == Enums.Status.InProgress);
                    return tasks;
                case Enums.Status.Done:
                    tasks = tasks.FindAll(t => t.Status == Enums.Status.Done);
                    return tasks;
                default:
                    break;
            }

            return tasks;
        }

        public async Task<AppTask> AddTask(string taskDescription)
        {
            if (string.IsNullOrWhiteSpace(taskDescription))
            {
                throw new ArgumentException("Task description cannot be null or empty.");
            }

            List<AppTask> tasks = await LoadTasksFromFile();
            
            var task = new AppTask
            {
                Id = GenerateTaskId(tasks),
                Description = taskDescription,
                Status = Enums.Status.Todo,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        
            tasks.Add(task);

            await SaveTasksToFile(tasks);

            return task;
        }

        public async Task<AppTask> UpdateTask(int taskId, string taskDescription)
        {
            if (string.IsNullOrWhiteSpace(taskDescription))
            {
                throw new ArgumentException("Task description cannot be null or empty.");
            }

            List<AppTask> tasks = await LoadTasksFromFile();

            var task = tasks.Find(t => t.Id == taskId);

            if (task is not null)
            {
                task.Description = taskDescription;
                task.UpdatedAt = DateTime.UtcNow;
                await SaveTasksToFile(tasks);
                return task;

            }
            else
            {
                throw new KeyNotFoundException($"Task with ID {taskId} not found.");
            }
        }

        public async Task<AppTask> UpdateTaskStatus(int taskId, Enums.Status status)
        {
            List<AppTask> tasks = await LoadTasksFromFile();
            var task = tasks.Find(t => t.Id == taskId);
            if (task is not null)
            {
                task.Status = status;
                task.UpdatedAt = DateTime.UtcNow;
                await SaveTasksToFile(tasks);
                return task;
            }
            else
            {
                throw new KeyNotFoundException($"Task with ID {taskId} not found.");
            }
        }

        public async Task DeleteTask(int taskId)
        {
            List<AppTask> tasks = await LoadTasksFromFile();

            if (tasks.Find(t => t.Id == taskId) is null)
            {
                throw new KeyNotFoundException($"Task with ID {taskId} not found.");
            }

            tasks.RemoveAll(t => t.Id == taskId);

            await SaveTasksToFile(tasks);
        }

        public static bool TryParseStatus(string arg, out Status status)
        {
            switch (arg)
            {
                case "todo":
                    status = Enums.Status.Todo;
                    return true;
                case "in-progress":
                    status = Enums.Status.InProgress;
                    return true;
                case "done":
                    status = Enums.Status.Done;
                    return true;
                default:
                    status = default;
                    return false;
            }
        }

        private static int GenerateTaskId(List<AppTask> tasks)
        {
            return tasks.Count == 0 ? 1 : tasks.Max(t => t.Id) + 1;
        } 

        private async Task SaveTasksToFile(List<AppTask> tasks)
        {
            var tempPath = filePath + ".tmp";

            await using (FileStream tempStream = File.Create(tempPath))
            {
                await JsonSerializer.SerializeAsync(tempStream, tasks, _jsonOption);
            }

            File.Move(tempPath, filePath, true);
        }

        private async Task<List<AppTask>> LoadTasksFromFile()
        {
            List<AppTask> tasks = [];

            if (File.Exists(filePath))
            {
                await using FileStream stream = File.OpenRead(filePath);
                try
                {
                    tasks = await JsonSerializer.DeserializeAsync<List<AppTask>>(stream, _jsonOption) ?? [];
                }
                catch (JsonException)
                {
                    throw new InvalidOperationException("The tasks.json file is empty or contains invalid JSON.");
                }
            }
            else
            {
                await SaveTasksToFile(tasks);

            }

            return tasks;
        }
    }
}
