using DotNet.Testcontainers.Builders;
using FirecrackerSharp.Management;
using FluentAssertions;

namespace FirecrackerSharp.Host.Ssh.Tests;

public class SshHostSocketManagerTests : SshServerFixture
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

    [Fact]
    public async Task GetAsync_ShouldReturnOk()
    {
        var socket = await ConnectToUdsAsync();
        var response = await socket.GetAsync<DataRecord>("get/ok");
        response.IsError.Should().BeFalse();
        response.TryUnwrap<DataRecord>()?
            .Field.Should().Be(1);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnBadRequest()
    {
        var socket = await ConnectToUdsAsync();
        var response = await socket.GetAsync<DataRecord>("get/bad-request");
        response.IsError.Should().BeTrue();
        var (errorType, _) = response.TryUnwrapError();
        errorType.Should().Be(ManagementResponseType.BadRequest);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnInternalServerError()
    {
        var socket = await ConnectToUdsAsync();
        var response = await socket.GetAsync<DataRecord>("get/error");
        response.IsError.Should().BeTrue();
        var (errorType, _) = response.TryUnwrapError();
        errorType.Should().Be(ManagementResponseType.InternalError);
    }

    private async Task<IHostSocket> ConnectToUdsAsync()
    {
        await InitializeUdsAsync(shouldRun: true);
        var socket = IHostSocketManager.Current.Connect(SocketAddress, "http://localhost");
        return socket;
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

// ReSharper disable once ClassNeverInstantiated.Global
public record DataRecord(int Field);