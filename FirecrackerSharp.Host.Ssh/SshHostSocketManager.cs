namespace FirecrackerSharp.Host.Ssh;

internal class SshHostSocketManager(ConnectionPool connectionPool, CurlConfiguration curlConfiguration) : IHostSocketManager
{
    public IHostSocket Connect(string socketAddress, string baseAddress)
    {
        if (!connectionPool.Sftp.Exists(socketAddress))
        {
            throw new SocketDoesNotExistException($"The \"{socketAddress}\" socket does not exist");
        }
        
        return new SshHostSocket(connectionPool, curlConfiguration, baseAddress, socketAddress);
    }
}