using System;
using System.Collections.Generic;
using System.Text;
using TaskTracker.Enums;
using TaskTracker.Models;

namespace TaskTracker.Interfaces
{
    public interface ITaskService
    {
        Task<List<AppTask>> GetTasks(Status? filter = null);
        Task<AppTask> AddTask(string taskDescription);
        Task<AppTask> UpdateTask(int taskId, string taskDescription);
        Task<AppTask> UpdateTaskStatus(int taskId, Enums.Status status);
        Task DeleteTask(int taskId);
    }
}
