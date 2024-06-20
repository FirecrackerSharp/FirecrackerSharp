using FluentAssertions;

namespace FirecrackerSharp.Host.Ssh.Tests;

public class SshHostProcessManagerTests : SshHostFixture
{
    [Fact]
    public async Task LaunchProcess_ShouldHaveCorrectOutput()
    {
        var process = IHostProcessManager.Current.LaunchProcess("useradd;echo exitsignal", "--help");
        await Task.Delay(500);
        var exited = await process.WaitForExitAsync(TimeSpan.FromSeconds(1), "exitsignal");

        exited.Should().BeTrue();
        process.CurrentOutput.Should().Contain("Usage: useradd [options] LOGIN");
    }
    
    [Fact]
    public void IsEscalated_ShouldBeTrue()
    {
        IHostProcessManager.Current.IsEscalated.Should().BeTrue();
    }
}