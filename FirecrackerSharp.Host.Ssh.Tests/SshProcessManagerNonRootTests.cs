using FluentAssertions;

namespace FirecrackerSharp.Host.Ssh.Tests;

public class SshProcessManagerNonRootTests() : SshServerFixture("regular", "regular")
{
    [Fact]
    public async Task LaunchProcess_ShouldHaveCorrectOutput()
    {
        const string executable = "ls";
        const string args = "--help";
        var expectedOutput = SshClient.RunCommand(executable + " " + args)?.Result;

        var process = IHostProcessManager.Current.LaunchProcess(executable, args);
        var actualOutput = await process.KillAndReadAsync();

        actualOutput.Should().Be(expectedOutput);
    }
}