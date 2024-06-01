using AutoFixture.Xunit2;
using FirecrackerSharp.Tests.Fixtures;
using FirecrackerSharp.Tty;
using FluentAssertions;

namespace FirecrackerSharp.Tests;

public class VmTtyTests : SingleVmFixture
{
    [Fact]
    public async Task LaunchingCommands_ShouldWork()
    {
        var shell = await Vm.TtyManager.StartShellAsync();
        
        await shell.StartCommandAsync("echo \"test\" > /tmp/file.txt");

        var catCommand = await shell.StartCommandAsync("cat /tmp/file.txt", CaptureMode.Stdout);
        var capturedOutput = await catCommand.CaptureOutputAsync();

        capturedOutput.Should().NotBeNull();
        capturedOutput.Should().Be("test");
    }

    [Fact]
    public async Task CaptureOutputAsync_ShouldIgnoreStderrWhenNotSpecified()
    {
        var shell = await Vm.TtyManager.StartShellAsync();

        var command = await shell.StartCommandAsync("logger -s error", CaptureMode.Stdout);
        var capturedOutput = await command.CaptureOutputAsync();
        
        capturedOutput.Should().NotBeNull();
        capturedOutput.Should().BeEmpty();
    }

    [Fact]
    public async Task CaptureOutputAsync_ShouldCaptureBothStdoutAndStderr()
    {
        var shell = await Vm.TtyManager.StartShellAsync();

        var command = await shell.StartCommandAsync($"mv /tmp/{Guid.NewGuid()} /tmp/{Guid.NewGuid()}", CaptureMode.StdoutPlusStderr);
        var capturedOutput = await command.CaptureOutputAsync();

        capturedOutput.Should().NotBeNull();
        capturedOutput.Should().EndWith("No such file or directory");
    }

    [Fact]
    public async Task CancelCommandAsync_ShouldUnblockShell()
    {
        var shell = await Vm.TtyManager.StartShellAsync();

        var command = await shell.StartCommandAsync("top");
        await command.CancelAsync();

        var subsequentCommand = await shell.StartCommandAsync("echo \"test\"", CaptureMode.Stdout);
        var commandOutput = await subsequentCommand.CaptureOutputAsync();

        commandOutput.Should().Be("test");
    }
}