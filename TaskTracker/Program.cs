using TaskTracker.Services;
using TaskTracker.Interfaces;
using TaskTracker;

Console.WriteLine("Task Tracker CLI");

ITaskService taskService = new TaskService();
var runner = new CommandRunner(taskService);

if (args.Length > 0)
{
    try
    {
        await runner.RunCommand(args);

    }
    catch (Exception ex)
    {
        CommandRunner.ShowError(ex.Message);
    }
}
else
{
    CommandRunner.ShowError("No command provided.");
}