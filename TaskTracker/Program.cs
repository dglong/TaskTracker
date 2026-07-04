using TaskTracker.Services;
using TaskTracker.Interfaces;
using TaskTracker;

Console.WriteLine("Task Tracker CLI");

ITaskService taskService = new TaskService();
var runner = new CommandRunner(taskService);

if (args.Length == 0)
{
    return CommandRunner.ShowError("No command provided.");
}

try
{
    return await runner.RunCommand(args);
}
catch (Exception ex) when (ex is KeyNotFoundException or ArgumentException or InvalidOperationException)
{
    return CommandRunner.ShowError(ex.Message);
}
