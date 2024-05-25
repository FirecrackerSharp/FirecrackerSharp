using FluentAssertions;

namespace FirecrackerSharp.Host.Ssh.Tests;

public class SshProcessManagerRootTests : SshServerFixture
{
    [Fact]
    public async Task LaunchRootProcess_ShouldHaveCorrectOutput()
    {
        const string executable = "useradd";
        const string args = "--help";
        var expectedOutput = SshClient.RunCommand(executable + " " + args)?.Result;

        var process = IHostProcessManager.Current.LaunchProcess(executable, args);
        var actualOutput = await process.KillAndReadAsync();

        actualOutput.Should().Be(expectedOutput);
    }
}