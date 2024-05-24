using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using FirecrackerSharp.Host.Ssh;
using Renci.SshNet;

namespace FirecrackerSharp.Host.Local.Tests;

public class SshServerFixture : IAsyncLifetime
{
    private static IContainer? Container { get; set; }
    private static ConnectionInfo ConnectionInfo { get; set; } = null!;

    protected static SshClient SshClient { get; set; } = null!;
    protected static SftpClient SftpClient { get; set; } = null!;
    
    public async Task InitializeAsync()
    {
        if (Container != null) return;

        var hostSshPort = Random.Shared.Next(10000, 65536);
        Container = new ContainerBuilder()
            .WithImage("ssh_server:latest")
            .WithPortBinding(hostSshPort, 22)
            .Build();

        await Container.StartAsync();

        ConnectionInfo = new ConnectionInfo(
            "127.0.0.1", hostSshPort, "root", new PasswordAuthenticationMethod("root", "root123"));
        
        SshHost.Configure(
            new ConnectionPoolConfiguration(
                ConnectionInfo,
                SshConnectionAmount: 2,
                SftpConnectionAmount: 2,
                KeepAliveInterval: TimeSpan.FromSeconds(1)),
            CurlConfiguration.Default);

        SshClient = new SshClient(ConnectionInfo);
        SshClient.Connect();
        
        SftpClient = new SftpClient(ConnectionInfo);
        SftpClient.Connect();
    }

    public async Task DisposeAsync()
    {
        if (Container != null)
        {
            await Container.StopAsync();
        }
        
        SshClient.Disconnect();
        SftpClient.Disconnect();
    }
}