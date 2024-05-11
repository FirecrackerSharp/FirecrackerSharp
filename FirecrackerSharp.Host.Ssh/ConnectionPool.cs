using Renci.SshNet;

namespace FirecrackerSharp.Host.Ssh;

internal class ConnectionPool : IDisposable
{
    private static readonly List<ConnectionPool> _connectionPools = [];
    
    internal readonly ConnectionInfo ConnectionInfo;

    private readonly List<SshClient> _sshConnections = [];
    private readonly List<SftpClient> _sftpConnections = [];
    private readonly uint _upkeep;

    internal SshClient Ssh
    {
        get
        {
            var missing = _sshConnections.Count(x => !x.IsConnected);
            for (var i = 0; i < missing; ++i)
            {
                _sshConnections.Add(NewUnmanagedSshConnection());
            }
            return _sshConnections.First();
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
            return _sftpConnections.First();
        }
    }
    
    internal ConnectionPool(ConnectionInfo connectionInfo, uint upkeep)
    {
        _connectionPools.Add(this);
        
        ConnectionInfo = connectionInfo;
        _upkeep = upkeep;

        for (var i = 0; i < upkeep; ++i)
        {
            _sshConnections.Add(NewUnmanagedSshConnection());
            _sftpConnections.Add(NewUnmanagedSftpConnection());
        }
    }

    internal SshClient NewUnmanagedSshConnection()
    {
        var sshClient = new SshClient(ConnectionInfo);
        sshClient.Connect();
        return sshClient;
    }

    private SftpClient NewUnmanagedSftpConnection()
    {
        var sftpClient = new SftpClient(ConnectionInfo);
        sftpClient.Connect();
        return sftpClient;
    }

    public void Dispose()
    {
        _sshConnections.ForEach(x => x.Disconnect());
        _sftpConnections.ForEach(x => x.Disconnect());
    }

    internal static void DisposeAll()
    {
        _connectionPools.ForEach(x => x.Dispose());
    }
}