using System;
using System.Collections.Generic;
using System.Text;
using TaskTracker.Enums;
using TaskTracker.Models;
using TaskTracker.Services;


namespace TaskTracker.Test
{
    public class TaskServiceTests: IDisposable
    {
        public readonly string _testFile;
        public readonly TaskService _testTaskService;
        public TaskServiceTests() 
        {
            _testFile = Path.Combine(Path.GetTempPath(), $"test_tasks{Guid.NewGuid()}.json");
            _testTaskService = new TaskService(_testFile);

        }
        public void Dispose() 
        {
            File.Delete(_testFile);
        }

        [Fact]
        public async Task AddTask_ToEmptyStore_AssignIdOne()
        {
            AppTask one = await _testTaskService.AddTask("Test task");

            Assert.Equal(1, one.Id);
        }

        [Fact]
        public async Task AddTask_Multiple_IncrementsId()
        {
            AppTask one = await _testTaskService.AddTask("Test task 1");
            AppTask two = await _testTaskService.AddTask("Test task 2");

            Assert.Equal(2, two.Id);
        }

        [Fact]
        public async Task AddTask_StatusTodo_AndTimeStamp()
        {
            AppTask one = await _testTaskService.AddTask("Test task 1");

            Assert.Equal(Enums.Status.Todo, one.Status);
            Assert.NotEqual(default(DateTime), one.CreatedAt);
            Assert.NotEqual(default(DateTime), one.UpdatedAt);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]

        public async Task AddTask_EmptyOrWhiteSpace_ThrowArgumentException(string description)
        {
            await Assert.ThrowsAsync<ArgumentException>(async () => await _testTaskService.AddTask(description));
        }

        [Fact]
        public async Task AddTask_PersistToFile()
        {
            AppTask one = await _testTaskService.AddTask("Test task 1");

            Assert.True(File.Exists(_testFile));
        }

        [Fact]
        public async Task SaveTasks_LeaveNoTmpResidue()
        {
            _ = await _testTaskService.AddTask("Test task 1");

            Assert.False(File.Exists(_testFile + ".tmp"));
        }

        [Theory]
        [InlineData(Enums.Status.Todo)]
        [InlineData(Enums.Status.InProgress)]
        [InlineData(Enums.Status.Done)]
        public async Task GetTasks_WithStatusFiltering_ReturnOnlyMatching(Enums.Status status)
        {
            _ = await _testTaskService.AddTask("Test task 1");
            _ = await _testTaskService.AddTask("Test task 2");
            _ = await _testTaskService.AddTask("Test task 3");

            await _testTaskService.UpdateTaskStatus(2, Enums.Status.InProgress);
            await _testTaskService.UpdateTaskStatus(3, Enums.Status.Done);


            List<AppTask> tasks = await _testTaskService.GetTasks(status);

            Assert.Single(tasks);
            Assert.Equal(status, tasks[0].Status);
        }

        [Fact]
        public async Task GetTasks_NoFilter_ReturnAll()
        {
            _ = await _testTaskService.AddTask("Test task 1");
            _ = await _testTaskService.AddTask("Test task 2");
            _ = await _testTaskService.AddTask("Test task 3");

            await _testTaskService.UpdateTaskStatus(2, Enums.Status.InProgress);
            await _testTaskService.UpdateTaskStatus(3, Enums.Status.Done);

            List<AppTask> tasks = await _testTaskService.GetTasks();

            Assert.Equal(3, tasks.Count);
        }

        [Fact]
        public async Task GetTasks_MissingFile_CreateEmptyStore()
        {
            Assert.False(File.Exists(_testFile));

            List<AppTask> tasks = await _testTaskService.GetTasks();

            Assert.Empty(tasks);
            Assert.True(File.Exists(_testFile));
        }

        [Fact]
        public async Task UpdateTask_ExistingId_ChangeDescription()
        {
            AppTask task = await _testTaskService.AddTask("Test task 1");

            AppTask updatedTask = await _testTaskService.UpdateTask(task.Id, "Test task changed");

            Assert.NotEqual(updatedTask.Description, task.Description);
            Assert.NotEqual(updatedTask.UpdatedAt, task.UpdatedAt);
        }

        [Fact]
        public async Task UpdateTask_MissingId_ThrowsKeyNotFound()
        {
            await Assert.ThrowsAsync<KeyNotFoundException>(async () => await _testTaskService.UpdateTask(1, "test update"));
        }

        [Theory]
        [InlineData(Enums.Status.InProgress)]
        [InlineData(Enums.Status.Done)]
        public async Task UpdateStatus_ExistingId_ChangesStatus(Enums.Status status)
        {
            AppTask task = await _testTaskService.AddTask("Test task 1");

            AppTask updatedTask = await _testTaskService.UpdateTaskStatus(task.Id, status);

            Assert.NotEqual(updatedTask.Status, task.Status);
        }

        [Fact]
        public async Task DeleteTask_ExistingId_RemovesIt()
        {
            AppTask task = await _testTaskService.AddTask("Test task 1");

            List<AppTask> tasks = await _testTaskService.GetTasks();

            Assert.NotEmpty(tasks);

            await _testTaskService.DeleteTask(task.Id);

            tasks = await _testTaskService.GetTasks();

            Assert.Empty(tasks);
        }

        [Fact]
        public async Task DeleteTask_MissingId_ThrowsKeyNotFound()
        {
            await Assert.ThrowsAsync<KeyNotFoundException>(async () => await _testTaskService.DeleteTask(1));
        }
    }
}
