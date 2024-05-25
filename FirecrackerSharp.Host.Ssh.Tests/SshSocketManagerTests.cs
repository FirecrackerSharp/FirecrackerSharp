using DotNet.Testcontainers.Builders;
using FluentAssertions;

namespace FirecrackerSharp.Host.Ssh.Tests;

public class SshSocketManagerTests : SshServerFixture
{
    private const string SocketAddress = "/tmp/uds-listener.sock";
    
    [Fact]
    public async Task Connect_ShouldNotThrow_ForExistingSocket()
    {
        await InitializeUdsAsync(shouldRun: true);
        var action = () => IHostSocketManager.Current.Connect(SocketAddress, "http://localhost");
        action.Should().NotThrow();
    }

    [Fact]
    public async Task Connect_ShouldThrow_ForNonExistentSocket()
    {
        await InitializeUdsAsync(shouldRun: false);
        var action = () => IHostSocketManager.Current.Connect(SocketAddress, "http://localhost");
        action.Should().Throw<SocketDoesNotExistException>();
    }
    
    private async Task InitializeUdsAsync(bool shouldRun)
    {
        if (SftpClient.Exists(SocketAddress))
        {
            SftpClient.DeleteFile(SocketAddress);
        }

        if (!shouldRun)
        {
            SshClient.RunCommand("pkill -f uds-listener");
            return;
        }
        
        const string binaryPath = "/tmp/uds-listener.bin";

        if (!SftpClient.Exists(binaryPath))
        {
            await using var inputStream = File.OpenRead(Path.Join(CommonDirectoryPath.GetProjectDirectory().DirectoryPath,
                "uds-listener.bin"));
            var result = SftpClient.BeginUploadFile(inputStream, binaryPath);
            result.AsyncWaitHandle.WaitOne();
            
            SftpClient.ChangePermissions(binaryPath, mode: 777);
        }
        
        var command = SshClient.CreateCommand($"DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 {binaryPath}");
        command.BeginExecute();
        await Task.Delay(100); // wait for application to start up and allocate the UDS
    }
}