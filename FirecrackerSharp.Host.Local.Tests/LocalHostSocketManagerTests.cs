using System.Diagnostics;
using AutoFixture.Xunit2;
using FirecrackerSharp.Management;
using FluentAssertions;

namespace FirecrackerSharp.Host.Local.Tests;

public class LocalHostSocketManagerTests : LocalHostFixture
{
    private static Process? _process;
    private const string SocketAddress = "/tmp/uds-listener.sock";

    [Fact]
    public async Task Connect_ShouldNotThrow_ForExistingSocket()
    {
        await WithUdsAsync(_ =>
        {
            var action = () => IHostSocketManager.Current.Connect(SocketAddress, "http://localhost");
            action.Should().NotThrow<SocketDoesNotExistException>();
            return Task.CompletedTask;
        });
    }

    [Fact]
    public void Connect_ShouldThrow_ForNonExistentSocket()
    {
        _process?.Kill();
        var action = () => IHostSocketManager.Current.Connect(SocketAddress, "http://localhost");
        action.Should().Throw<SocketDoesNotExistException>();
    }

    [Fact]
    public async Task GetAsync_ShouldPerformRequests()
    {
        await WithUdsAsync(async socket =>
        {
            var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token;
            
            var okResponse = await socket.GetAsync<DataRecord>("/get/ok", cancellationToken);
            okResponse.IsSuccess.Should().BeTrue();
            
            var badRequestResponse = await socket.GetAsync<DataRecord>("/get/bad-request", cancellationToken);
            badRequestResponse.Type.Should().Be(ResponseType.BadRequest);
            
            var errorResponse = await socket.GetAsync<DataRecord>("/get/error", cancellationToken);
            errorResponse.Type.Should().Be(ResponseType.InternalError);
        });
    }

    [Theory, AutoData]
    public async Task PatchAsync_ShouldPerformRequests(DataRecord dataRecord)
    {
        await WithUdsAsync(async socket =>
        {
            var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token;
            
            var okResponse = await socket.PatchAsync("/patch/ok", dataRecord, cancellationToken);
            okResponse.IsSuccess.Should().BeTrue();

            var badRequestResponse = await socket.PatchAsync("/patch/bad-request", dataRecord, cancellationToken);
            badRequestResponse.Type.Should().Be(ResponseType.BadRequest);

            var errorResponse = await socket.PatchAsync("/patch/error", dataRecord, cancellationToken);
            errorResponse.Type.Should().Be(ResponseType.InternalError);
        });
    }

    [Theory, AutoData]
    public async Task PutAsync_ShouldPerformRequests(DataRecord dataRecord)
    {
        await WithUdsAsync(async socket =>
        {
            var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token;
            
            var okResponse = await socket.PutAsync("/put/ok", dataRecord, cancellationToken);
            okResponse.IsSuccess.Should().BeTrue();

            var badRequestResponse = await socket.PutAsync("/put/bad-request", dataRecord, cancellationToken);
            badRequestResponse.Type.Should().Be(ResponseType.BadRequest);

            var errorResponse = await socket.PutAsync("/put/error", dataRecord, cancellationToken);
            errorResponse.Type.Should().Be(ResponseType.InternalError);
        });
    }

    private static async Task WithUdsAsync(Func<IHostSocket, Task> asyncAction)
    {
        await RunUdsListenerAsync();
        
        var socket = IHostSocketManager.Current.Connect(SocketAddress, "http://localhost");
        await asyncAction(socket);
        
        File.Delete(SocketAddress);
        _process?.Kill();
    }
    
    private static async Task RunUdsListenerAsync()
    {
        if (File.Exists(SocketAddress))
        {
            File.Delete(SocketAddress);
        }
        
        const string binaryPath = "/opt/testdata/uds-listener.bin";

        _process?.Kill();

        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = binaryPath,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        _process.Start();

        await Task.Delay(200);
    }
}

// ReSharper disable once ClassNeverInstantiated.Global
// ReSharper disable once NotAccessedPositionalProperty.Global
public record DataRecord(int Field);