using FirecrackerSharp.Host;
using FirecrackerSharp.Tests.Helpers;
using FirecrackerSharp.Tty.OutputBuffering;
using FluentAssertions;

namespace FirecrackerSharp.Tests.OutputBuffering;

public class FileOutputBufferTests : MinimalFixture
{
    private const string Filename = "/tmp/fob.txt";
    
    [Fact]
    public void Open_ShouldCreateFile_OnAppHost()
    {
        Cleanup();
        var buffer = new FileOutputBuffer(Filename);
        File.Exists(Filename).Should().BeFalse();
        buffer.Open();
        File.Exists(Filename).Should().BeTrue();
    }

    [Fact]
    public void Open_ShouldCreateFile_OnVmHost()
    {
        Cleanup();
        var buffer = new FileOutputBuffer(Filename, onAppHost: false);
        IHostFilesystem.Current.FileOrDirectoryExists(Filename).Should().BeFalse();
        buffer.Open();
        IHostFilesystem.Current.FileOrDirectoryExists(Filename).Should().BeTrue();
    }

    [Fact]
    public async Task Receive_ShouldWriteToFile_OnAppHost()
    {
        Cleanup();
        var buffer = new FileOutputBuffer(Filename);
        buffer.Open();
        buffer.Receive("a");
        (await File.ReadAllTextAsync(Filename)).Should().Be("a");
    }

    [Fact]
    public async Task Receive_ShouldWriteToFile_OnVmHost()
    {
        Cleanup();
        var buffer = new FileOutputBuffer(Filename, onAppHost: false);
        buffer.Open();
        buffer.Receive("a");
        (await IHostFilesystem.Current.ReadTextFileAsync(Filename)).Should().Be("a");
    }

    [Fact]
    public async Task Commit_ShouldCommitDeferredWrites_OnAppHost()
    {
        Cleanup();
        var buffer = new FileOutputBuffer(Filename, deferWriteUntilCommit: true);
        buffer.Open();
        buffer.Receive("a");
        buffer.Receive("b");
        (await File.ReadAllTextAsync(Filename)).Should().BeEmpty();
        buffer.Commit();
        (await File.ReadAllTextAsync(Filename)).Should().Be("ab");
    }

    [Fact]
    public async Task Commit_ShouldCommitDeferredWrites_OnVmHost()
    {
        Cleanup();
        var buffer = new FileOutputBuffer(Filename, onAppHost: false, deferWriteUntilCommit: true);
        buffer.Open();
        buffer.Receive("a");
        buffer.Receive("b");
        (await IHostFilesystem.Current.ReadTextFileAsync(Filename)).Should().BeEmpty();
        buffer.Commit();
        (await IHostFilesystem.Current.ReadTextFileAsync(Filename)).Should().Be("ab");
    }

    private static void Cleanup()
    {
        if (IHostFilesystem.Current.FileOrDirectoryExists(Filename)) IHostFilesystem.Current.DeleteFile(Filename);
        if (File.Exists(Filename)) File.Delete(Filename);
    }
}