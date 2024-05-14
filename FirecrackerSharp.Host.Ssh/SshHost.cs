using Serilog;

namespace FirecrackerSharp.Host.Ssh;

/// <summary>
/// An SDK Linux host that is a remote Linux machine or VM, which is connected to through SSH and SFTP with the
/// SSH.NET library internally.
///
/// This host is strictly for development if accessing /dev/kvm is not a possibility (Windows, macOS or restricted
/// Linux) or for production in scenarios where there's no feasible alternative to remote communication.
/// </summary>
public static class SshHost
{
    private static ConnectionPool? _currentConnectionPool;
    
    /// <summary>
    /// Configure the SSH host to be used internally by the SDK.
    /// </summary>
    /// <param name="connectionPoolConfiguration">The <see cref="ConnectionPoolConfiguration"/> for SSH and SFTP</param>
    /// <param name="curlConfiguration">The <see cref="CurlConfiguration"/> for UDS communication</param>
    public static void Configure(ConnectionPoolConfiguration connectionPoolConfiguration, CurlConfiguration curlConfiguration)
    {
        _currentConnectionPool = new ConnectionPool(connectionPoolConfiguration);
        IHostFilesystem.Current = new SshHostFilesystem(_currentConnectionPool);
        IHostProcessManager.Current = new SshHostProcessManager(_currentConnectionPool);
        IHostSocketManager.Current = new SshHostSocketManager(_currentConnectionPool, curlConfiguration);
        
        Log.Information("Using remote SSH host for FirecrackerSharp");
    }

    /// <summary>
    /// Dispose of the connection pool used internally to establish and keep up SSH and SFTP connections.
    ///
    /// This is necessary after usage of this host is finished, since otherwise all SSH and SFTP would run even though
    /// they are unused (until sshd terminates them).
    /// </summary>
    /// <exception cref="ArgumentNullException">If there's no connection pool to dispose of (<see cref="Configure"/>
    /// hasn't been called)</exception>
    public static void Dispose()
    {
        if (_currentConnectionPool is null)
            throw new ArgumentNullException(nameof(_currentConnectionPool),
                "No SSH & SFTP connection pool to dispose of");
        
        _currentConnectionPool.Dispose();
    }
}