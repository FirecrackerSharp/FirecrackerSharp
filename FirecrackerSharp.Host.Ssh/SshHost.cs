using Renci.SshNet;
using Serilog;

namespace FirecrackerSharp.Host.Ssh;

public static class SshHost
{
    public static void Configure(ConnectionInfo connectionInfo, uint concurrentConnections)
    {
        var connectionManager = new ConnectionPool(connectionInfo, concurrentConnections);
        IHostFilesystem.Current = new SshHostFilesystem(connectionManager);
        IHostProcessManager.Current = new SshHostProcessManager(connectionManager);
        
        Log.Information("Using remote SSH host for FirecrackerSharp");
    }

    public static void Dispose()
    {
        ConnectionPool.DisposeAll();
    }
}