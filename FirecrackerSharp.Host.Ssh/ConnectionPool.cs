using Renci.SshNet;
using Serilog;

namespace FirecrackerSharp.Host.Ssh;

internal class ConnectionPool : IDisposable
{
    private static readonly ILogger Logger = Log.ForContext<ConnectionPool>();
    private static readonly Random Random = new();
    
    internal readonly ConnectionInfo ConnectionInfo;

    private readonly List<SshClient> _sshConnections = [];
    private readonly List<SftpClient> _sftpConnections = [];
    private readonly ConnectionPoolConfiguration _configuration;

    internal SshClient Ssh
    {
        get
        {
            var missing = _sshConnections.Count(x => !x.IsConnected);
            for (var i = 0; i < missing; ++i)
            {
                _sshConnections.Add(NewUnmanagedSshConnection());
            }

            if (missing > 0)
            {
                Logger.Debug("Re-established {missing} SSH connection(s)", missing);
            }

            return _sshConnections[Random.Next(_sshConnections.Count)];
        }
    }

    internal SftpClient Sftp
    {
        get
        {
            var missing = _sftpConnections.Count(x => !x.IsConnected);
            for (var i = 0; i < missing; ++i)
            {
                _sftpConnections.Add(NewUnmanagedSftpConnection());
            }
            
            if (missing > 0)
            {
                Logger.Debug("Re-established {missing} SFTP connection(s)", missing);
            }

            return _sftpConnections[Random.Next(_sftpConnections.Count)];
        }
    }
    
    internal ConnectionPool(ConnectionPoolConfiguration configuration)
    {
        ConnectionInfo = configuration.ConnectionInfo;
        _configuration = configuration;
        
        for (var i = 0; i < configuration.SshConnectionAmount; ++i)
        {
            _sshConnections.Add(NewUnmanagedSshConnection());
        }

        for (var i = 0; i < configuration.SftpConnectionAmount; ++i)
        {
            _sftpConnections.Add(NewUnmanagedSftpConnection());
        }
        
        Logger.Information("Established SSH & SFTP connection pool of {sshAmount} SSH and {sftpAmount} SFTP connections",
            configuration.SshConnectionAmount, configuration.SftpConnectionAmount);
    }

    internal SshClient NewUnmanagedSshConnection()
    {
        var sshClient = new SshClient(ConnectionInfo);
        sshClient.Connect();
        sshClient.KeepAliveInterval = _configuration.KeepAliveInterval;
        return sshClient;
    }

    private SftpClient NewUnmanagedSftpConnection()
    {
        var sftpClient = new SftpClient(ConnectionInfo);
        sftpClient.Connect();
        sftpClient.KeepAliveInterval = _configuration.KeepAliveInterval;
        return sftpClient;
    }

    public void Dispose()
    {
        _sshConnections.ForEach(x => x.Disconnect());
        _sftpConnections.ForEach(x => x.Disconnect());
        Logger.Information("Disposed of all connections in SSH & SFTP connection pool");
    }
}