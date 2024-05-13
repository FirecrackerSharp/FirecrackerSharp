using Serilog;

namespace FirecrackerSharp.Host.Ssh;

public static class SshHost
{
    private static ConnectionPool? _currentConnectionPool;
    
    public static void Configure(ConnectionPoolConfiguration connectionPoolConfiguration, CurlConfiguration curlConfiguration)
    {
        _currentConnectionPool = new ConnectionPool(connectionPoolConfiguration);
        IHostFilesystem.Current = new SshHostFilesystem(_currentConnectionPool);
        IHostProcessManager.Current = new SshHostProcessManager(_currentConnectionPool);
        IHostSocketManager.Current = new SshHostSocketManager(_currentConnectionPool, curlConfiguration);
        
        Log.Information("Using remote SSH host for FirecrackerSharp");
    }

    public static void Dispose()
    {
        if (_currentConnectionPool is null)
            throw new ArgumentNullException(nameof(_currentConnectionPool),
                "No SSH & SFTP connection pool to dispose of");
        
        _currentConnectionPool.Dispose();
    }
}