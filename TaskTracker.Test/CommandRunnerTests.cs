using System;
using System.Collections.Generic;
using System.Text;
using TaskTracker.Interfaces;
using TaskTracker.Services;

namespace TaskTracker.Test
{
    public class CommandRunnerTests: IDisposable
    {
        public readonly string _testFile;
        private readonly ITaskService _testTaskService;
        private readonly CommandRunner _testCommandRunner;
        public CommandRunnerTests()
        {
            _testFile = Path.Combine(Path.GetTempPath(), $"test_tasks{Guid.NewGuid()}.json");
            _testTaskService = new TaskService(_testFile);
            _testCommandRunner = new CommandRunner(_testTaskService);
        }
        public void Dispose() 
        {
            File.Delete(_testFile);
        }

        [Fact]
        public async Task RunCommand_ValidArgs_ReturnsZero()
        {
            int exitCode = await _testCommandRunner.RunCommand(["list"]);

            Assert.Equal(0, exitCode);
        }

        [Fact]
        public async Task RunCommand_InValidArgs_ReturnsOne()
        {
            int exitCode = await _testCommandRunner.RunCommand(["invalid"]);

            Assert.Equal(1, exitCode);
        }

        [Fact]
        public async Task RunCommand_NonNumericId_ReturnsZero()
        {
            _ = await _testCommandRunner.RunCommand(["add", "test task"]);
            int exitCode = await _testCommandRunner.RunCommand(["delete", "1"]);

            Assert.Equal(0, exitCode);
        }

        [Fact]
        public async Task RunCommand_TwoFewArgs_ReturnsOne()
        {
            int exitCode = await _testCommandRunner.RunCommand(["add"]);

            Assert.Equal(1, exitCode);
        }

        [Fact]
        public async Task RunCommand_TwoManyArgs_ReturnsOne()
        {
            int exitCode = await _testCommandRunner.RunCommand(["add", "test task", "more args"]);

            Assert.Equal(1, exitCode);
        }

        [Fact]
        public async Task RunCommand_InvalidListFilter_ReturnsOne()
        {
            int exitCode = await _testCommandRunner.RunCommand(["list", "test task"]);

            Assert.Equal(1, exitCode);
        }
    }
}
