namespace FirecrackerSharp.Host.Ssh;

internal sealed class SshHostSocketManager(ConnectionPool connectionPool, CurlConfiguration curlConfiguration) : IHostSocketManager
{
    public IHostSocket Connect(string socketPath, string baseUri)
    {
        if (!connectionPool.Sftp.Exists(socketPath))
        {
            throw new SocketDoesNotExistException($"The \"{socketPath}\" socket does not exist");
        }
        
        return new SshHostSocket(connectionPool, curlConfiguration, baseUri, socketPath);
    }
}