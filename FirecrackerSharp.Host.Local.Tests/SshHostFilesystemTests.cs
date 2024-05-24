using AutoFixture.Xunit2;
using FluentAssertions;

namespace FirecrackerSharp.Host.Local.Tests;

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
}