using System.Diagnostics;
using FluentAssertions;

namespace FirecrackerSharp.Host.Local.Tests;

public class LocalHostProcessManagerTests : LocalHostFixture
{
    [Fact]
    public async Task LaunchProcess_ShouldHaveCorrectOutput()
    {
        var expectedProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = "-c \"ls --help\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };
        expectedProcess.Start();
        await Task.Delay(100);
        expectedProcess.Kill();
        var expectedOutput = await expectedProcess.StandardOutput.ReadToEndAsync();

        var actualProcess = IHostProcessManager.Current.LaunchProcess("/bin/bash", "-c \"ls --help\"");
        await Task.Delay(100);
        var actualOutput = await actualProcess.KillAndReadAsync();

        actualOutput.Should().Be(expectedOutput);
    }
    
    [Fact]
    public void IsEscalated_ShouldCorrespondToUsername()
    {
        var isActuallyEscalated = Environment.UserName == "root";
        var isEscalated = IHostProcessManager.Current.IsEscalated;
        isEscalated.Should().Be(isActuallyEscalated);
    }
}