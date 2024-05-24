using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using FirecrackerSharp.Host.Ssh;
using Renci.SshNet;

namespace FirecrackerSharp.Host.Local.Tests;

public class SshServerFixture : IAsyncLifetime
{
    private static IContainer? Container { get; set; }
    private static ConnectionInfo ConnectionInfo { get; set; } = null!;

    protected SshClient SshClient { get; private set; } = null!;
    protected SftpClient SftpClient { get; private set; } = null!;
    
    public async Task InitializeAsync()
    {
        if (Container != null)
        {
            Connect();
            return;
        }

        var hostSshPort = Random.Shared.Next(10000, 65536);
        Container = new ContainerBuilder()
            .WithImage("ssh_server:latest")
            .WithPortBinding(hostSshPort, 22)
            .WithAutoRemove(true)
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
        
        Connect();
    }

    private void Connect()
    {
        SshClient = new SshClient(ConnectionInfo);
        SshClient.Connect();
        
        SftpClient = new SftpClient(ConnectionInfo);
        SftpClient.Connect();
    }

    public Task DisposeAsync()
    {
        SshClient.Disconnect();
        SftpClient.Disconnect();
        
        return Task.CompletedTask;
    }
}