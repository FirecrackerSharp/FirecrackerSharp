using FluentAssertions;

namespace FirecrackerSharp.Host.Ssh.Tests;

public class SshHostProcessManagerTests : SshHostFixture
{
    [Fact]
    public async Task LaunchProcess_ShouldHaveCorrectOutput()
    {
        var process = IHostProcessManager.Current.LaunchProcess("useradd", "--help");
        await Task.Delay(500);
        var exited = await process.WaitForGracefulExitAsync(TimeSpan.FromSeconds(1));

        exited.Should().BeTrue();
        process.CurrentOutput.Should().Contain("Usage: useradd [options] LOGIN");
    }
    
    [Fact]
    public void IsEscalated_ShouldBeTrue()
    {
        IHostProcessManager.Current.IsEscalated.Should().BeTrue();
    }
}