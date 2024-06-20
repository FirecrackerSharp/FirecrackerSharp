using AutoFixture.Xunit2;
using FirecrackerSharp.Management;
using FluentAssertions;

namespace FirecrackerSharp.Host.Ssh.Tests;

public class SshHostSocketManagerTests : SshHostFixture
{
    private const string SocketAddress = "/tmp/uds-listener.sock";
    private static CancellationToken CancellationToken => new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token;
    
    [Fact]
    public async Task Connect_ShouldNotThrow_ForExistingSocket()
    {
        await RunUdsListenerAsync(shouldActuallyRun: true);
        var action = () => IHostSocketManager.Current.Connect(SocketAddress, "http://localhost");
        action.Should().NotThrow<SocketDoesNotExistException>();
    }

    [Fact]
    public async Task Connect_ShouldThrow_ForNonExistentSocket()
    {
        await RunUdsListenerAsync(shouldActuallyRun: false);
        var action = () => IHostSocketManager.Current.Connect(SocketAddress, "http://localhost");
        action.Should().Throw<SocketDoesNotExistException>();
    }

    [Fact]
    public async Task GetAsync_ShouldReturnOk()
    {
        var socket = await ConnectToUdsAsync();
        var response = await socket.GetAsync<DataRecord>("get/ok", CancellationToken);
        response.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetAsync_ShouldReturnBadRequest()
    {
        var socket = await ConnectToUdsAsync();
        var response = await socket.GetAsync<DataRecord>("get/bad-request", CancellationToken);
        response.Type.Should().Be(ResponseType.BadRequest);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnInternalServerError()
    {
        var socket = await ConnectToUdsAsync();
        var response = await socket.GetAsync<DataRecord>("get/error", CancellationToken);
        response.Type.Should().Be(ResponseType.InternalError);
    }

    [Theory, AutoData]
    public async Task PatchAsync_ShouldReturnOk(DataRecord dataRecord)
    {
        var socket = await ConnectToUdsAsync();
        var response = await socket.PatchAsync("patch/ok", dataRecord, CancellationToken);
        response.IsSuccess.Should().BeTrue();
    }

    [Theory, AutoData]
    public async Task PatchAsync_ShouldReturnBadRequest(DataRecord dataRecord)
    {
        var socket = await ConnectToUdsAsync();
        var response = await socket.PatchAsync("patch/bad-request", dataRecord, CancellationToken);
        response.Type.Should().Be(ResponseType.BadRequest);
    }

    [Theory, AutoData]
    public async Task PatchAsync_ShouldReturnInternalServerError(DataRecord dataRecord)
    {
        var socket = await ConnectToUdsAsync();
        var response = await socket.PatchAsync("patch/error", dataRecord, CancellationToken);
        response.Type.Should().Be(ResponseType.InternalError);
    }

    [Theory, AutoData]
    public async Task PutAsync_ShouldReturnOk(DataRecord dataRecord)
    {
        var socket = await ConnectToUdsAsync();
        var response = await socket.PutAsync("put/ok", dataRecord, CancellationToken);
        response.IsSuccess.Should().BeTrue();
    }

    [Theory, AutoData]
    public async Task PutAsync_ShouldReturnBadRequest(DataRecord dataRecord)
    {
        var socket = await ConnectToUdsAsync();
        var response = await socket.PutAsync("put/bad-request", dataRecord, CancellationToken);
        response.Type.Should().Be(ResponseType.BadRequest);
    }
    
    [Theory, AutoData]
    public async Task PutAsync_ShouldReturnInternalServerError(DataRecord dataRecord)
    {
        var socket = await ConnectToUdsAsync();
        var response = await socket.PutAsync("put/error", dataRecord, CancellationToken);
        response.Type.Should().Be(ResponseType.InternalError);
    }

    private async Task<IHostSocket> ConnectToUdsAsync()
    {
        await RunUdsListenerAsync(shouldActuallyRun: true);
        var socket = IHostSocketManager.Current.Connect(SocketAddress, "http://localhost");
        return socket;
    }
    
    private async Task RunUdsListenerAsync(bool shouldActuallyRun)
    {
        if (SftpClient.Exists(SocketAddress))
        {
            SftpClient.DeleteFile(SocketAddress);
        }

        if (!shouldActuallyRun) return;

        const string binaryPath = "/opt/testdata/uds-listener.bin";
        const string outputPath = "/tmp/uds-listener.bin";

        if (!SftpClient.Exists(binaryPath))
        {
            await using var inputStream = File.OpenRead(binaryPath);
            var result = SftpClient.BeginUploadFile(inputStream, outputPath);
            result.AsyncWaitHandle.WaitOne();
            
            SftpClient.ChangePermissions(outputPath, mode: 777);
        }
        
        var command = SshClient.CreateCommand($"DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 {outputPath}");
        command.BeginExecute();
        await Task.Delay(100); // wait for application to start up and allocate the UDS
    }
}

// ReSharper disable once ClassNeverInstantiated.Global
// ReSharper disable once NotAccessedPositionalProperty.Global
public record DataRecord(int Field);