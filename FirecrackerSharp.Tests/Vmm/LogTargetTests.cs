using System.Text;
using FirecrackerSharp.Host;
using FirecrackerSharp.Lifecycle;
using FirecrackerSharp.Tests.Helpers;
using FluentAssertions;

namespace FirecrackerSharp.Tests.Vmm;

public class LogTargetTests : MinimalFixture
{
    [Fact]
    public void ShouldWrite_ToStringBuilder()
    {
        var stringBuilder = new StringBuilder();
        var logTarget = ILogTarget.ToStringBuilder(stringBuilder);
        logTarget.Receive("abc");
        stringBuilder.ToString().Should().Be("abc");
    }

    [Fact]
    public void ShouldWrite_ToStream()
    {
        var path = $"/tmp/{Guid.NewGuid()}";
        
        var fileStream = File.Open(path, FileMode.CreateNew);
        var logTarget = ILogTarget.ToStream(fileStream);
        logTarget.Receive("abc");
        fileStream.Close();

        File.ReadAllText(path).Should().Be("abc");
        File.Delete(path);
    }

    [Fact]
    public void ShouldWrite_ToFile_OnAppHost()
    {
        var path = $"/tmp/{Guid.NewGuid()}";

        var logTarget = ILogTarget.ToFile(path, onAppHost: true);
        logTarget.Receive("abc");

        File.ReadAllText(path).Should().Be("abc");
        File.Delete(path);
    }

    [Fact]
    public async Task ShouldWrite_ToFile_OnVmHost()
    {
        var path = $"/tmp/{Guid.NewGuid()}";

        var logTarget = ILogTarget.ToFile(path, onAppHost: false);
        logTarget.Receive("abc");

        (await IHostFilesystem.Current.ReadTextFileAsync(path)).Should().Be("abc");
        IHostFilesystem.Current.DeleteFile(path);
    }

    [Fact]
    public void ShouldWrite_ToAggregate()
    {
        var path = $"/tmp/{Guid.NewGuid()}";
        var stringBuilder = new StringBuilder();

        var logTarget = ILogTarget.ToAggregate([ILogTarget.ToStringBuilder(stringBuilder), ILogTarget.ToFile(path)]);
        logTarget.Receive("abc");

        stringBuilder.ToString().Should().Be("abc");
        File.ReadAllText(path).Should().Be("abc");
        File.Delete(path);
    }
}