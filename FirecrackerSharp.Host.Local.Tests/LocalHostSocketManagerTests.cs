using System.Diagnostics;
using FluentAssertions;

namespace FirecrackerSharp.Host.Local.Tests;

public class LocalHostSocketManagerTests : LocalHostFixture
{
    private static Process? _process;
    private const string SocketAddress = "/tmp/uds-listener.sock";

    [Fact]
    public async Task Connect_ShouldNotThrow_ForExistingSocket()
    {
        await WithUdsListenerAsync(() =>
        {
            var action = () => IHostSocketManager.Current.Connect(SocketAddress, "http://localhost");
            action.Should().NotThrow<SocketDoesNotExistException>();
        });
    }

    [Fact]
    public void Connect_ShouldThrow_ForNonExistentSocket()
    {
        var action = () => IHostSocketManager.Current.Connect(SocketAddress, "http://localhost");
        action.Should().Throw<SocketDoesNotExistException>();
    }

    private async Task WithUdsListenerAsync(Action action)
    {
        await RunUdsListenerAsync();
        action();
        File.Delete(SocketAddress);
        _process?.Kill();
    }
    
    private static async Task RunUdsListenerAsync()
    {
        if (File.Exists(SocketAddress))
        {
            File.Delete(SocketAddress);
        }
        
        const string binaryPath = "/opt/firecracker-sharp/uds-listener.bin";

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

        await Task.Delay(100);
    }
}