using DotNet.Testcontainers.Builders;
using FluentAssertions;

namespace FirecrackerSharp.Host.Ssh.Tests;

public class SshSocketManagerTests : SshServerFixture
{
    [Fact]
    public async Task Connect_ShouldNotThrow_ForExistingSocket()
    {
        var address = await RunUdsListenerAsync();

        var action = () => IHostSocketManager.Current.Connect(address, "http://localhost");

        action.Should().NotThrow();
    }
    
    private async Task<string> RunUdsListenerAsync()
    {
        const string binaryPath = "/tmp/uds-listener.bin";
        const string socketPath = "/tmp/uds-listener.sock";

        if (!SftpClient.Exists(binaryPath))
        {
            await using var inputStream = File.OpenRead(Path.Join(CommonDirectoryPath.GetProjectDirectory().DirectoryPath,
                "uds-listener.bin"));
            SftpClient.UploadFile(inputStream, binaryPath);
            SftpClient.ChangePermissions(binaryPath, mode: 777);
        }

        if (SftpClient.Exists(socketPath))
        {
            SftpClient.DeleteFile(socketPath);
        }

        SshClient.RunCommand($".{binaryPath}");

        return socketPath;
    }
}