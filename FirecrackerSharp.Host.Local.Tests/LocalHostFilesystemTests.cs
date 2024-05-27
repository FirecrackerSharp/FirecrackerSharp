using AutoFixture.Xunit2;
using FluentAssertions;

namespace FirecrackerSharp.Host.Local.Tests;

public class LocalHostFilesystemTests : LocalHostFixture
{
    [Theory, AutoData]
    public async Task WriteTextFileAsync_ShouldPersist(string content)
    {
        var path = Path.GetTempFileName();
        await IHostFilesystem.Current.WriteTextFileAsync(path, content);

        File.Exists(path).Should().BeTrue();
        (await File.ReadAllTextAsync(path)).Should().Be(content);
    }

    [Theory, AutoData]
    public async Task WriteBinaryFileAsync_ShouldPersist(byte[] content)
    {
        var path = Path.GetTempFileName();
        await IHostFilesystem.Current.WriteBinaryFileAsync(path, content);

        File.Exists(path).Should().BeTrue();
        (await File.ReadAllBytesAsync(path)).Should().BeEquivalentTo(content);
    }

    [Theory, AutoData]
    public async Task ReadTextFileAsync_ShouldRead(string content)
    {
        var path = Path.GetTempFileName();
        await File.WriteAllTextAsync(path, content);

        var actualContent = await IHostFilesystem.Current.ReadTextFileAsync(path);
        actualContent.Should().Be(content);
    }

    [Theory, AutoData]
    public async Task CopyFileAsync_ShouldCopy(byte[] content)
    {
        var path1 = Path.GetTempFileName();
        var path2 = Path.GetTempFileName();
        await File.WriteAllBytesAsync(path1, content);

        await IHostFilesystem.Current.CopyFileAsync(path1, path2);
        var copiedContent = await File.ReadAllBytesAsync(path2);
        copiedContent.Should().BeEquivalentTo(content);
    }

    [Fact]
    public void GetTemporaryFilename_ShouldBeValid()
    {
        var filename = IHostFilesystem.Current.GetTemporaryFilename();
        var action = () => File.Create(filename).Close();
        action.Should().NotThrow();
    }

    [Fact]
    public void CreateTextFile_ShouldPersist()
    {
        var filename = Path.GetTempFileName();
        IHostFilesystem.Current.CreateTextFile(filename);
        File.Exists(filename).Should().BeTrue();
    }

    [Theory, AutoData]
    public void CreateDirectory_ShouldPersist(Guid id)
    {
        var path = $"/tmp/{id}";
        IHostFilesystem.Current.CreateDirectory(path);
        Directory.Exists(path).Should().BeTrue();
    }

    [Theory, AutoData]
    public void GetSubdirectories_ShouldOnlyReturnSubdirectories(Guid id)
    {
        var (path, subdirectories, _) = ArrangeFilesAndDirectories(id);
        var actualSubdirectories = IHostFilesystem.Current.GetSubdirectories(path);
        actualSubdirectories.Should().BeEquivalentTo(subdirectories);
    }

    [Theory, AutoData]
    public void GetFiles_ShouldOnlyReturnSubFiles(Guid id)
    {
        var (path, _, subFiles) = ArrangeFilesAndDirectories(id);
        var actualSubFiles = IHostFilesystem.Current.GetFiles(path);
        actualSubFiles.Should().BeEquivalentTo(subFiles);
    }

    [Fact]
    public void MakeFileExecutable_ShouldApplyPermissions()
    {
        var filename = Path.GetTempFileName();
        File.Create(filename).Close();
        
        IHostFilesystem.Current.MakeFileExecutable(filename);

        // these tests are only runnable on linux, so ignore
#pragma warning disable CA1416
        File.GetUnixFileMode(filename).Should().HaveFlag(UnixFileMode.UserExecute);
#pragma warning restore CA1416
    }

    [Fact]
    public void DeleteFile_ShouldWork()
    {
        var filename = Path.GetTempFileName();
        File.Create(filename).Close();
        
        IHostFilesystem.Current.DeleteFile(filename);
        
        File.Exists(filename).Should().BeFalse();
    }

    [Theory, AutoData]
    public void DeleteDirectoryRecursively_ShouldRemoveEverything(Guid id1, Guid id2)
    {
        var path = $"/tmp/{id1}";
        var subPath = $"/tmp/{id1}/{id2}";
        Directory.CreateDirectory(path);
        File.Create(subPath).Close();
        
        IHostFilesystem.Current.DeleteDirectoryRecursively(path);

        Directory.Exists(path).Should().BeFalse();
        File.Exists(subPath).Should().BeFalse();
    }

    [Fact]
    public void CreateTemporaryDirectory_ShouldEnsureDirectoryExists()
    {
        var path = IHostFilesystem.Current.CreateTemporaryDirectory();
        Directory.Exists(path).Should().BeTrue();
    }

    [Theory, AutoData]
    public void JoinPaths_ShouldUseCorrectSeparator(Guid[] ids)
    {
        var stringIds = ids.Select(x => x.ToString()).ToArray();
        var expectedPath = string.Join("/", stringIds);

        var actualPath = IHostFilesystem.Current.JoinPaths(stringIds);
        actualPath.Should().Be(expectedPath);
    }

    private static (string, List<string>, List<string>) ArrangeFilesAndDirectories(Guid id)
    {
        var path = $"/tmp/{id}";
        Directory.CreateDirectory(path);
        var subdirectories = new List<string>();
        var subFiles = new List<string>();
        for (var i = 0; i < 10; ++i)
        {
            subdirectories.Add($"/tmp/{id}/{Guid.NewGuid()}");
            subFiles.Add($"/tmp/{id}/{Guid.NewGuid()}");
        }
        subdirectories.ForEach(x => Directory.CreateDirectory(x));
        subFiles.ForEach(x => File.CreateText(x).Close());

        return (path, subdirectories, subFiles);
    }
}