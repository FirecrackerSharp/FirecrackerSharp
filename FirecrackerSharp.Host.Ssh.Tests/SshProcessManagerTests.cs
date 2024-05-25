using FluentAssertions;

namespace FirecrackerSharp.Host.Ssh.Tests;

public class SshProcessManagerTests : SshServerFixture
{
    [Fact]
    public async Task LaunchProcess_ShouldHaveCorrectOutput()
    {
        var expectedOutput = SshClient.RunCommand("useradd --help")?.Result;

        var process = IHostProcessManager.Current.LaunchProcess("useradd", "--help");
        var actualOutput = await process.KillAndReadAsync();

        actualOutput.Should().Be(expectedOutput);
    }
    
    [Fact]
    public void IsEscalated_ShouldBeTrue()
    {
        IHostProcessManager.Current.IsEscalated.Should().BeTrue();
    }
}