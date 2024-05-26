using AutoFixture.Xunit2;
using FluentAssertions;

namespace FirecrackerSharp.Host.Ssh.Tests;

public class SshHostFilesystemTests : SshServerFixture
{
    [Theory, AutoData]
    public async Task WriteTextFileAsync_ShouldPersist(Guid id, string content)
    {
        var filename = $"/tmp/{id}";
        await IHostFilesystem.Current.WriteTextFileAsync(filename, content);

        var actualContent = SftpClient.ReadAllText(filename);
        actualContent.Should().NotBeNull();
        actualContent.Should().Be(content);
    }

    [Theory, AutoData]
    public async Task WriteBinaryFileAsync_ShouldPersist(Guid id, byte[] content)
    {
        var filename = $"/tmp/{id}";
        await IHostFilesystem.Current.WriteBinaryFileAsync(filename, content);

        var actualContent = SftpClient.ReadAllBytes(filename);
        actualContent.Should().NotBeNull();
        actualContent.Should().BeEquivalentTo(content);
    }

    [Theory, AutoData]
    public async Task ReadTextFileAsync_ShouldReturnContent(Guid id, string content)
    {
        var filename = $"/tmp/{id}";
        SftpClient.WriteAllText(filename, content);

        var actualContent = await IHostFilesystem.Current.ReadTextFileAsync(filename);
        actualContent.Should().Be(content);
    }

    [Theory, AutoData]
    public async Task CopyFileAsync_ShouldPerformCopy(Guid id1, Guid id2, byte[] content)
    {
        var filename1 = $"/tmp/{id1}";
        var filename2 = $"/tmp/{id2}";
        SftpClient.WriteAllBytes(filename1, content);

        await IHostFilesystem.Current.CopyFileAsync(filename1, filename2);
        var actualContent1 = SftpClient.ReadAllBytes(filename1);
        var actualContent2 = SftpClient.ReadAllBytes(filename2);

        actualContent1.Should().NotBeNull();
        actualContent2.Should().NotBeNull();
        actualContent1.Should().BeEquivalentTo(actualContent2).And.BeEquivalentTo(content);
    }

    [Fact]
    public void GetTemporaryFilename_ShouldReturnValidFilename()
    {
        var filename = IHostFilesystem.Current.GetTemporaryFilename();
        SftpClient.Create(filename); // throws and fails test if filename is invalid
    }

    [Theory, AutoData]
    public void CreateDirectory_ShouldCreateDirectory(Guid id)
    {
        var path = $"/tmp/{id}";
        IHostFilesystem.Current.CreateDirectory(path);
        
        SftpClient.Exists(path).Should().BeTrue();
        SftpClient.Get(path).IsDirectory.Should().BeTrue();
    }

    [Theory, AutoData]
    public void CreateTextFile_ShouldCreateEmptyFile(Guid id)
    {
        var path = $"/tmp/{id}";
        IHostFilesystem.Current.CreateTextFile(path);

        SftpClient.Exists(path).Should().BeTrue();
        SftpClient.ReadAllText(path).Should().BeEmpty();
    }

    [Theory, AutoData]
    public void GetSubdirectories_ShouldReturnOnlySubdirectories(Guid id)
    {
        var (path, subdirectories, _) = ArrangeFilesAndDirectories(id);
        var actualSubdirectories = IHostFilesystem.Current.GetSubdirectories(path);
        actualSubdirectories.Should().BeEquivalentTo(subdirectories);
    }

    [Theory, AutoData]
    public void GetFiles_ShouldOnlyReturnSubFiles(Guid id)
    {
        var (path, _, subFiles) = ArrangeFilesAndDirectories(id);
        var actualFiles = IHostFilesystem.Current.GetFiles(path);
        actualFiles.Should().BeEquivalentTo(subFiles);
    }

    [Theory, AutoData]
    public void MakeFileExecutable_ShouldApplyPermissions(Guid id)
    {
        var path = $"/tmp/{id}";
        SftpClient.CreateText(path);
        
        IHostFilesystem.Current.MakeFileExecutable(path);
        
        SftpClient.Get(path).OwnerCanExecute.Should().BeTrue();
    }

    [Theory, AutoData]
    public void DeleteFile_ShouldWork(Guid id)
    {
        var path = $"/tmp/{id}";
        SftpClient.CreateText(path);
        
        IHostFilesystem.Current.DeleteFile(path);

        SftpClient.Exists(path).Should().BeFalse();
    }

    [Theory, AutoData]
    public void DeleteDirectoryRecursively_ShouldRemoveEverything(Guid id1, Guid id2)
    {
        var path = $"/tmp/{id1}";
        var subPath = $"/tmp/{id1}/{id2}";
        SftpClient.CreateDirectory(path);
        SftpClient.CreateText(subPath);
        
        IHostFilesystem.Current.DeleteDirectoryRecursively(path);

        SftpClient.Exists(subPath).Should().BeFalse();
        SftpClient.Exists(path).Should().BeFalse();
    }

    [Fact]
    public void CreateTemporaryDirectory_ShouldPersist()
    {
        var directory = IHostFilesystem.Current.CreateTemporaryDirectory();
        
        SftpClient.Exists(directory).Should().BeTrue();
        SftpClient.Get(directory).IsDirectory.Should().BeTrue();
    }

    [Theory, AutoData]
    public void JoinPaths_ShouldUseCorrectSeparator(Guid[] ids)
    {
        var stringIds = ids.Select(x => x.ToString()).ToArray();
        var expectedPath = "/" + string.Join("/", stringIds);
        
        var actualPath = IHostFilesystem.Current.JoinPaths(stringIds);

        actualPath.Should().Be(expectedPath);
    }

    private (string, List<string>, List<string>) ArrangeFilesAndDirectories(Guid id)
    {
        var path = $"/tmp/{id}";
        SftpClient.CreateDirectory(path);
        var subdirectories = new List<string>();
        var subFiles = new List<string>();
        for (var i = 0; i < 10; ++i)
        {
            subdirectories.Add($"/tmp/{id}/{Guid.NewGuid()}");
            subFiles.Add($"/tmp/{id}/{Guid.NewGuid()}");
        }
        subdirectories.ForEach(x => SftpClient.CreateDirectory(x));
        subFiles.ForEach(x => SftpClient.CreateText(x));

        return (path, subdirectories, subFiles);
    }
}